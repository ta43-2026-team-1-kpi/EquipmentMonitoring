# Експлуатація

Команди виконуються з кореня репозиторію в PowerShell. Щоб не повторювати `-f`, задайте набір файлів один раз на сесію:

```powershell
$env:COMPOSE_FILE = 'docker-compose.yaml;docker-compose.prod.yaml;docker-compose.monitoring.yaml'
$env:COMPOSE_PROFILES = 'demo'   # симулятор датчиків; без нього дашборд показує тільки реальні дані
```

На Linux і macOS роздільник у `COMPOSE_FILE` — `:`.

## Перший запуск

```powershell
Copy-Item .env.example .env      # несекретні параметри: імена, порти
./scripts/new-secrets.ps1        # випадкові паролі в secrets/
docker compose up -d --build --wait
```

| Адреса | Вхід |
|---|---|
| http://localhost:8080/swagger | — |
| http://localhost:5050 | `PGADMIN_DEFAULT_EMAIL` з `.env`, пароль з `secrets/pgadmin_password.txt`; сервер `EquipmentMonitoring` уже підключено |
| http://localhost:3000 | `admin`, пароль з `secrets/grafana_admin_password.txt`; дашборд «Моніторинг обладнання» відкривається одразу |

`postgres_password` діє тільки під час першої ініціалізації `postgres_data`. Якщо volume уже створено з іншим паролем (наприклад, базовим стеком з `.env`), або змініть пароль у базі, або почніть з нуля через `docker compose down -v` (це видаляє дані).

## Перевірка стеку

```powershell
./scripts/verify-stack.ps1 -ComposeFile docker-compose.yaml,docker-compose.prod.yaml,docker-compose.monitoring.yaml -Profile demo
```

Скрипт перевіряє доступність, бізнес-сценарій, безпеку, моніторинг, збереження даних після `restart` і `down`/`up`, а також відновлення з бекапу. Код виходу 1 означає, що якась перевірка не пройшла. Ту саму перевірку запускає CI на кожен PR.

## Резервні копії

Сервіс `backup` раз на `BACKUP_INTERVAL_SECONDS` (за замовчуванням добу) знімає `pg_dump` у volume `postgres_backups` і видаляє копії, старші за `BACKUP_RETENTION_DAYS` (7).

```powershell
docker compose exec backup /bin/sh /scripts/backup.sh --once     # зняти копію зараз
docker compose exec backup ls -lh /backups                       # список копій
docker compose cp backup:/backups/<файл>.dump .                  # забрати копію на хост
```

Відновлення замінює поточний вміст бази. API на цей час зупиняємо, щоб не було записів посередині:

```powershell
docker compose stop api
docker compose exec backup /bin/sh /scripts/restore.sh              # остання копія
docker compose exec backup /bin/sh /scripts/restore.sh <файл>.dump  # конкретна копія
docker compose up -d --wait
```

## Локальна розробка (F5)

```powershell
docker compose -f docker-compose.yaml -f docker-compose.dev.yaml up -d db
```

Далі F5 у Visual Studio. Профіль `Development` бере рядок підключення з `appsettings.Development.json` і застосовує міграції на старті. Облікові дані там збігаються з `.env.example`; якщо змінили їх у `.env`, перевизначте рядок через user-secrets або `ConnectionStrings__EquipmentDatabase`.

## Тести

```powershell
dotnet test                                      # усі тести; PostgreSQL-тести самі піднімають контейнер
dotnet test --filter "Category!=PostgreSQL"      # без Docker
```

## Діагностика

| Симптом | Що перевірити |
|---|---|
| `api` не стає healthy | `docker compose logs api`; `curl http://localhost:8080/health/ready` (503 — немає з'єднання з базою) |
| `password authentication failed` | пароль у `secrets/postgres_password.txt` не збігається з тим, яким ініціалізовано `postgres_data` (див. «Перший запуск») |
| `POSTGRES_DB must be set in .env` | немає `.env` або в ньому бракує змінної |
| `grafana` не стартує | `docker compose logs grafana-db-init` — роль створюється після того, як API застосував міграції |
| порожній дашборд | вибраний період часу; без профілю `demo` дані з'являються тільки з реальних запитів |
