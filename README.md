# EquipmentMonitoring

[![CI](https://github.com/ta43-2026-team-1-kpi/EquipmentMonitoring/actions/workflows/ci.yml/badge.svg)](https://github.com/ta43-2026-team-1-kpi/EquipmentMonitoring/actions/workflows/ci.yml)

## Призначення

**EquipmentMonitoring** — навчальний ASP.NET Core Web API-проєкт для моніторингу обладнання.

Реалізовані сценарії:
- **EPC-4** — реєстрація обладнання та перегляд переліку;
- **EPC-5** — додавання вимірювань і перегляд історії;
- **EPC-6** — межі (thresholds) і попередження (warnings);
- **EPC-7** — плани технічного обслуговування.

## Структура рішення

- **EquipmentMonitoring.API** — контролери, OpenAPI/Swagger, DI.
- **EquipmentMonitoring.Application** — DTO, валідація, сервіси, прикладні винятки.
- **EquipmentMonitoring.Domain** — сутності та enum.
- **EquipmentMonitoring.Infrastructure** — EF Core, конфігурації, репозиторії, міграції.
- **EquipmentMonitoring.Tests** — unit/integration тести (xUnit).

## Технології

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10
- SQLite (існуючий локальний/тестовий контур)
- PostgreSQL (контейнерний runtime для ЛР3)
- OpenAPI + Swagger UI
- xUnit

## Передумови

- Git
- .NET 10 SDK
- Visual Studio (або інший .NET IDE)
- Docker Desktop (WSL 2)
- Docker Compose (`docker compose`)

## ЛР3: запуск через Docker Compose (API + PostgreSQL + pgAdmin)

### 0) Клонування та вибір гілки ЛР3

```powershell
git clone https://github.com/ta43-2026-team-1-kpi/EquipmentMonitoring
Set-Location .\EquipmentMonitoring
git checkout feature/lab3-docker-postgresql
```

### 1) Підготовка `.env`

1. Створіть локальний файл (PowerShell):

```powershell
Copy-Item .env.example .env
```

2. Заповніть параметри:
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `PGADMIN_DEFAULT_EMAIL`
- `PGADMIN_DEFAULT_PASSWORD`
- `API_PORT`
- `PGADMIN_PORT`

Для pgAdmin використовуйте валідний email, наприклад `admin@example.com`.

Важливо: `PGADMIN_DEFAULT_PASSWORD` (вхід у веб-інтерфейс pgAdmin) та `POSTGRES_PASSWORD` (пароль користувача PostgreSQL) — це різні облікові дані, задавайте їх окремо.

### 2) Запуск однією командою

```powershell
docker compose up -d --build
```

Перевірка стану:

```powershell
docker compose ps
```

### 3) Адреси сервісів і порти

- Swagger UI: `http://localhost:<API_PORT>/swagger/index.html`
- OpenAPI JSON: `http://localhost:<API_PORT>/openapi/v1.json`
- pgAdmin: `http://localhost:<PGADMIN_PORT>`

Якщо порт зайнятий, змініть `API_PORT` або `PGADMIN_PORT` у `.env` і перезапустіть сервіси (`docker compose up -d`).

### 4) Підключення pgAdmin до PostgreSQL

У pgAdmin (Create Server):
- **General → Name**: будь-яка локальна назва (наприклад `EquipmentMonitoring DB`)
- **Connection → Host name/address**: `db`
- **Connection → Port**: `5432`
- **Connection → Maintenance database**: значення `POSTGRES_DB` із локального `.env`
- **Connection → Username**: значення `POSTGRES_USER` із локального `.env`
- **Connection → Password**: значення `POSTGRES_PASSWORD` із локального `.env`

### 5) Міграції PostgreSQL

У контейнерному режимі API застосовує PostgreSQL-міграції на старті (`Database:ApplyMigrationsOnStartup=true`).
Помилка міграції зупиняє запуск API.

Під час запуску через Compose локальний .NET SDK не потрібен: API збирається у Docker image.

Для локального `dotnet build` / `dotnet test` SDK потрібен.

### 6) Build і тести

```powershell
dotnet build
dotnet test
```

Для запуску API поза Docker задайте PostgreSQL-рядок підключення через змінну середовища `ConnectionStrings__EquipmentDatabase`.

Тести з категорією `PostgreSQL` запускають контейнер PostgreSQL через Testcontainers, тому потребують запущеного Docker. Без Docker:

```powershell
dotnet test --filter "Category!=PostgreSQL"
```

Запуск API з Visual Studio (F5): підніміть лише БД з відкритим портом і запускайте профіль `Development`, рядок підключення для нього задано в `appsettings.Development.json`.

```powershell
docker compose -f docker-compose.yaml -f docker-compose.dev.yaml up -d db
```

### 7) Керування сервісами

```powershell
docker compose stop
docker compose start
docker compose restart
docker compose down
```

`docker compose down -v` видаляє named volumes і дані БД. Для перевірки персистентності в ЛР3 використовуйте `down` **без** `-v`.

### 8) Де зберігаються дані

- PostgreSQL: named volume `postgres_data`
- pgAdmin: named volume `pgadmin_data`

Демодані, їхні GUID/ID і локальний state-файл перевірок не передаються через Git.

### 9) Порядок перевірочних скриптів ЛР3

Запускати по черзі:

```powershell
.\scripts\lab3-functional-check.ps1
.\scripts\lab3-persistence-check.ps1
.\scripts\lab3-postgres-check.ps1
```

Другий і третій скрипт залежать від state-файла `%TEMP%\equipmentmonitoring-lab3-state.json`, який створює `lab3-functional-check.ps1`.

### 10) Запуск з Docker secrets і бекапами

1. Згенеруйте паролі (каталог `secrets/` не потрапляє в Git і Docker image):

```powershell
./scripts/new-secrets.ps1
```

2. Запустіть стек:

```powershell
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml up -d --build --wait
```

У цьому режимі паролі передаються як Docker secrets, а не змінними середовища; `db` доступний тільки з внутрішньої мережі `backend`; усі сервіси мають healthcheck і ліміти ресурсів. `--wait` повертає керування, коли всі сервіси здорові.

Важливо: пароль PostgreSQL застосовується лише під час першої ініціалізації volume. Якщо `postgres_data` уже створено з паролем із `.env`, запускайте з окремим іменем проєкту (`docker compose -p prod ...`) або видаліть volume (`docker compose down -v`, дані буде втрачено).

### 11) pgAdmin і Grafana

- pgAdmin: `http://localhost:<PGADMIN_PORT>`, вхід — `PGADMIN_DEFAULT_EMAIL` і пароль із `secrets/pgadmin_password.txt`. Сервер `EquipmentMonitoring` уже зареєстровано, налаштовувати підключення вручну не потрібно.
- Grafana: `http://localhost:3000` (порт задає `GRAFANA_PORT`), вхід — `admin` і пароль із `secrets/grafana_admin_password.txt`. Дашборд «Моніторинг обладнання» відкривається після входу; БД читається під роллю з правом лише на читання.

Grafana і симулятор датчиків (профіль `demo`, надсилає показники через API) підключаються окремим файлом:

```powershell
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml -f docker-compose.monitoring.yaml --profile demo up -d --wait
```

### 12) Бекапи

Сервіс `backup` раз на добу створює копію БД у volume `postgres_backups` і зберігає копії за останні 7 днів (`BACKUP_INTERVAL_SECONDS`, `BACKUP_RETENTION_DAYS` у `.env`).

```powershell
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml exec backup /bin/sh /scripts/backup.sh --once
```

Відновлення останньої копії (API на цей час зупиняється):

```powershell
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml stop api
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml exec backup /bin/sh /scripts/restore.sh
docker compose -f docker-compose.yaml -f docker-compose.prod.yaml up -d --wait
```

Відновлення конкретної копії, перелік копій, діагностика: [docs/operations.md](docs/operations.md). Схема мереж і сервісів: [docs/deployment.md](docs/deployment.md). Обґрунтування рішень: [docs/adr](docs/adr).

### 13) Автоматична перевірка стеку

```powershell
./scripts/verify-stack.ps1
```

Скрипт піднімає стек і перевіряє доступність сервісів, бізнес-сценарій, відсутність паролів у змінних середовища, ізоляцію БД, збереження даних після `restart` і `down`/`up`, відновлення з бекапу. Для перевірки разом із Grafana:

```powershell
./scripts/verify-stack.ps1 -ComposeFile docker-compose.yaml,docker-compose.prod.yaml,docker-compose.monitoring.yaml -Profile demo
```

Та сама перевірка виконується в CI для кожного PR.

Важливо: скрипт перезапускає стек і використовує ті самі volume, що й п. 10, тому обмеження щодо пароля PostgreSQL з п. 10 діє і тут. Для ізольованого прогону з видаленням даних після завершення: `./scripts/verify-stack.ps1 -ProjectName verify -Down` (базовий стек перед цим зупиніть: `docker compose down`).

## Endpoint-и

### EPC-4 (обладнання)
- `POST /api/equipment`
- `GET /api/equipment`

### EPC-5 (вимірювання)
- `POST /api/equipment/{equipmentId}/measurements`
- `GET /api/equipment/{equipmentId}/measurements`

### EPC-6 (межі та попередження)
- `GET /api/thresholds`
- `GET /api/thresholds/{measurementType}`
- `PUT /api/thresholds/{measurementType}`
- `DELETE /api/thresholds/{measurementType}`
- `GET /api/equipment/{equipmentId}/thresholds`
- `GET /api/equipment/{equipmentId}/thresholds/effective`
- `GET /api/equipment/{equipmentId}/thresholds/{measurementType}`
- `PUT /api/equipment/{equipmentId}/thresholds/{measurementType}`
- `DELETE /api/equipment/{equipmentId}/thresholds/{measurementType}`
- `GET /api/warnings`
- `GET /api/warnings/summary`
- `GET /api/warnings/{warningId}`
- `POST /api/warnings/{warningId}/acknowledge`

### EPC-7 (плани ТО)
- `POST /api/maintenance-plans`
- `GET /api/maintenance-plans`
- `PATCH /api/maintenance-plans/{maintenancePlanId}/status`

### Службові

- `GET /health/live` — процес API працює
- `GET /health/ready` — API готовий обробляти запити: перевіряє з'єднання з БД, повертає 503, якщо БД недоступна

## Типові проблеми

- API/pgAdmin перезапускаються: перевірте `docker compose logs api` / `docker compose logs pgadmin`.
- Swagger недоступний: перевірте `docker compose ps` і порт `API_PORT`.
- Тести падають по датах планів ТО: використовуйте відносні майбутні дати в тестах, не фіксовані календарні дати.
