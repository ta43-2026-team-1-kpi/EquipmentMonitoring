"""Sensor simulator for the EquipmentMonitoring API.

Registers a small fleet of equipment (idempotent, by serial number), sets per-equipment thresholds
and then periodically posts measurements through the public API, exactly like a real sensor gateway.
Values follow a slow daily-like wave with noise; occasional spikes cross the thresholds, so warnings
appear on their own. Only equipment-specific thresholds are touched, global ones are left alone.
"""

import json
import logging
import math
import os
import random
import signal
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone

API_URL = os.environ.get("API_URL", "http://api:8080").rstrip("/")
INTERVAL_SECONDS = float(os.environ.get("INTERVAL_SECONDS", "5"))
SPIKE_PROBABILITY = float(os.environ.get("SPIKE_PROBABILITY", "0.03"))
WAVE_PERIOD_SECONDS = float(os.environ.get("WAVE_PERIOD_SECONDS", "600"))

UNITS = {"Temperature": "°C", "Voltage": "V", "Load": "%"}

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("sensor-simulator")


@dataclass(frozen=True)
class Signal:
    baseline: float
    amplitude: float
    noise: float
    min_value: float | None
    max_value: float | None
    spike: float


@dataclass(frozen=True)
class Machine:
    serial: str
    name: str
    type: str
    location: str
    signals: dict[str, Signal]


FLEET = [
    Machine("SIM-CMP-001", "Compressor A1", "Compressor", "Workshop 1", {
        "Temperature": Signal(62, 8, 1.5, None, 85, 18),
        "Load": Signal(55, 15, 3, None, 90, 30),
    }),
    Machine("SIM-PMP-002", "Pump P-7", "Pump", "Workshop 1", {
        "Temperature": Signal(48, 6, 1.0, None, 70, 20),
        "Voltage": Signal(230, 3, 1.0, 215, 245, 18),
    }),
    Machine("SIM-MTR-003", "Motor M-2", "Motor", "Workshop 2", {
        "Voltage": Signal(400, 5, 1.5, 380, 420, 25),
        "Load": Signal(70, 10, 4, None, 95, 25),
    }),
    Machine("SIM-CNV-004", "Conveyor C-3", "Conveyor", "Warehouse", {
        "Load": Signal(40, 20, 4, None, 85, 45),
        "Temperature": Signal(35, 5, 1.0, None, 60, 25),
    }),
]


class ApiError(Exception):
    def __init__(self, message: str, status: int | None = None) -> None:
        super().__init__(message)
        self.status = status


def request(method: str, path: str, body: dict | None = None) -> object:
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(f"{API_URL}{path}", data=data, method=method)
    req.add_header("Accept", "application/json")
    if data is not None:
        req.add_header("Content-Type", "application/json")

    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            payload = response.read()
            is_json = "json" in response.headers.get("Content-Type", "")
            return json.loads(payload) if payload and is_json else None
    except urllib.error.HTTPError as error:
        detail = error.read().decode(errors="replace")[:300]
        raise ApiError(f"{method} {path} -> {error.code}: {detail}", error.code) from error
    except (urllib.error.URLError, TimeoutError, ConnectionError) as error:
        raise ApiError(f"{method} {path} -> {error}") from error


def wait_for_api() -> None:
    delay = 1.0
    while True:
        try:
            request("GET", "/health/ready")
            return
        except ApiError as error:
            log.info("waiting for API: %s", error)
            time.sleep(delay)
            delay = min(delay * 2, 15)


def register_fleet() -> dict[str, str]:
    existing = {e["serialNumber"]: e["id"] for e in request("GET", "/api/equipment")}
    ids = {}

    for machine in FLEET:
        if machine.serial not in existing:
            created = request("POST", "/api/equipment", {
                "name": machine.name,
                "type": machine.type,
                "serialNumber": machine.serial,
                "location": machine.location,
                "status": "Working",
            })
            existing[machine.serial] = created["id"]
            log.info("registered %s (%s)", machine.name, machine.serial)

        ids[machine.serial] = existing[machine.serial]
        for measurement_type, signal_ in machine.signals.items():
            request("PUT", f"/api/equipment/{ids[machine.serial]}/thresholds/{measurement_type}", {
                "minValue": signal_.min_value,
                "maxValue": signal_.max_value,
            })

    return ids


def sample(signal_: Signal, phase: float, now: float) -> float:
    value = signal_.baseline
    value += signal_.amplitude * math.sin(2 * math.pi * now / WAVE_PERIOD_SECONDS + phase)
    value += random.gauss(0, signal_.noise)

    if random.random() < SPIKE_PROBABILITY:
        value += signal_.spike if signal_.min_value is None or random.random() < 0.5 else -signal_.spike

    return round(value, 2)


def run() -> None:
    stopping = False

    def stop(*_: object) -> None:
        nonlocal stopping
        stopping = True

    signal.signal(signal.SIGTERM, stop)
    signal.signal(signal.SIGINT, stop)

    wait_for_api()
    ids = register_fleet()
    phases = {(m.serial, t): random.uniform(0, 2 * math.pi) for m in FLEET for t in m.signals}
    log.info("sending measurements for %d machines every %.0fs to %s", len(FLEET), INTERVAL_SECONDS, API_URL)

    while not stopping:
        started = time.monotonic()
        measured_at = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        sent = failed = 0
        missing_equipment = False

        for machine in FLEET:
            for measurement_type, signal_ in machine.signals.items():
                value = sample(signal_, phases[(machine.serial, measurement_type)], time.time())
                try:
                    request("POST", f"/api/equipment/{ids[machine.serial]}/measurements", {
                        "type": measurement_type,
                        "value": value,
                        "unit": UNITS[measurement_type],
                        "measuredAt": measured_at,
                    })
                    sent += 1
                except ApiError as error:
                    failed += 1
                    missing_equipment |= error.status == 404
                    log.warning("%s", error)

        log.info("sent %d measurements, %d failed", sent, failed)
        if missing_equipment:
            # Equipment was removed meanwhile (e.g. a database restore): register the fleet again.
            try:
                ids = register_fleet()
            except ApiError as error:
                log.warning("re-registration failed: %s", error)
        time.sleep(max(0.0, INTERVAL_SECONDS - (time.monotonic() - started)))

    log.info("stopped")


if __name__ == "__main__":
    run()
