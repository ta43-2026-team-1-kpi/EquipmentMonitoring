param(
    [string] $StatePath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$envPath = Join-Path $repoRoot '.env'

function Invoke-CheckedExternal {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Command,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $result = & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }

    return $result
}

function Get-EnvMap {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $map = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $map
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^(?<k>[^#=]+)=(?<v>.*)$') {
            $map[$matches.k.Trim()] = $matches.v.Trim()
        }
    }

    return $map
}

if ([string]::IsNullOrWhiteSpace($StatePath)) {
    $StatePath = Join-Path $env:TEMP 'equipmentmonitoring-lab3-state.json'
}

if (-not (Test-Path -LiteralPath $StatePath)) {
    throw "State file not found. Run scripts/lab3-functional-check.ps1 first: $StatePath"
}

$state = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
$envData = Get-EnvMap -Path $envPath

$db = $envData['POSTGRES_DB']
$user = $envData['POSTGRES_USER']

if ([string]::IsNullOrWhiteSpace($db) -or [string]::IsNullOrWhiteSpace($user)) {
    throw "POSTGRES_DB/POSTGRES_USER are missing in .env: $envPath"
}

$measurement1Value = [double]$state.Measurement1Value
$measurement2Value = [double]$state.Measurement2Value
$expectedWarningStatus = if ($state.PSObject.Properties.Name -contains 'WarningExpectedStatus') { [string]$state.WarningExpectedStatus } else { [string]$state.WarningStatusAfterAcknowledge }
if ([string]::IsNullOrWhiteSpace($expectedWarningStatus)) {
    $expectedWarningStatus = 'Acknowledged'
}

$sql = @"
\set ON_ERROR_STOP 1

DO `$`$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM "Equipment"
        WHERE "Id" = '$($state.EquipmentId)'::uuid
          AND "SerialNumber" = '$($state.EquipmentSerialExpectedNormalized)'
    ) THEN
        RAISE EXCEPTION 'Equipment row not found or serial mismatch for %', '$($state.EquipmentId)';
    END IF;
END `$`$;

DO `$`$
DECLARE
    m1 numeric;
    m2 numeric;
BEGIN
    SELECT "Value" INTO m1
    FROM "Measurements"
    WHERE "Id" = '$($state.Measurement1Id)'::uuid
      AND "EquipmentId" = '$($state.EquipmentId)'::uuid;

    IF m1 IS NULL THEN
        RAISE EXCEPTION 'Measurement1 not found: %', '$($state.Measurement1Id)';
    END IF;

    IF ABS(m1 - $measurement1Value) > 0.000001 THEN
        RAISE EXCEPTION 'Measurement1 value mismatch. expected %, got %', $measurement1Value, m1;
    END IF;

    SELECT "Value" INTO m2
    FROM "Measurements"
    WHERE "Id" = '$($state.Measurement2Id)'::uuid
      AND "EquipmentId" = '$($state.EquipmentId)'::uuid;

    IF m2 IS NULL THEN
        RAISE EXCEPTION 'Measurement2 not found: %', '$($state.Measurement2Id)';
    END IF;

    IF ABS(m2 - $measurement2Value) > 0.000001 THEN
        RAISE EXCEPTION 'Measurement2 value mismatch. expected %, got %', $measurement2Value, m2;
    END IF;
END `$`$;

DO `$`$
DECLARE
    w_status text;
    w_ack timestamp with time zone;
BEGIN
    SELECT "Status"::text, "AcknowledgedAtUtc" INTO w_status, w_ack
    FROM "Warnings"
    WHERE "Id" = '$($state.WarningId)'::uuid
      AND "EquipmentId" = '$($state.EquipmentId)'::uuid
      AND "MeasurementId" = '$($state.WarningMeasurementId)'::uuid;

    IF w_status IS NULL THEN
        RAISE EXCEPTION 'Warning not found for expected warning/equipment/measurement ids.';
    END IF;

    IF w_status <> '$expectedWarningStatus' THEN
        RAISE EXCEPTION 'Warning status mismatch. expected %, got %', '$expectedWarningStatus', w_status;
    END IF;

    IF w_ack IS NULL THEN
        RAISE EXCEPTION 'Warning acknowledged timestamp is NULL.';
    END IF;
END `$`$;

DO `$`$
DECLARE
    p_status text;
BEGIN
    SELECT "Status"::text INTO p_status
    FROM "MaintenancePlans"
    WHERE "Id" = '$($state.MaintenancePlanId)'::uuid
      AND "EquipmentId" = '$($state.EquipmentId)'::uuid;

    IF p_status IS NULL THEN
        RAISE EXCEPTION 'Maintenance plan not found: %', '$($state.MaintenancePlanId)';
    END IF;

    IF p_status <> '$($state.MaintenancePlanFinalStatus)' THEN
        RAISE EXCEPTION 'Maintenance plan status mismatch. expected %, got %', '$($state.MaintenancePlanFinalStatus)', p_status;
    END IF;
END `$`$;

SELECT 'lab3-postgres-check: OK' AS result;
"@

$sqlFile = [System.IO.Path]::GetTempFileName()
try {
    Set-Content -LiteralPath $sqlFile -Value $sql -Encoding UTF8
    Invoke-CheckedExternal -Name 'docker cp sql into db container' -Command {
        docker cp "$sqlFile" equipment-monitoring-db:/tmp/lab3-check.sql
    } | Out-Null

    $psqlOutput = Invoke-CheckedExternal -Name 'docker exec psql assertions' -Command {
        docker exec equipment-monitoring-db psql -v ON_ERROR_STOP=1 -U "$user" -d "$db" -f /tmp/lab3-check.sql
    }

    [pscustomobject]@{
        RepoRoot = $repoRoot
        StatePath = $StatePath
        Database = $db
        User = $user
        Result = 'OK'
        PsqlOutput = ($psqlOutput -join [Environment]::NewLine)
    } | ConvertTo-Json -Depth 5
}
finally {
    Remove-Item -LiteralPath $sqlFile -ErrorAction SilentlyContinue
}
