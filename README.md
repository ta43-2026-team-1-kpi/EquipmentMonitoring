# EquipmentMonitoring

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

### 1) Підготовка `.env`

1. Створіть локальний файл:

```bash
cp .env.example .env
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

### 2) Запуск однією командою

```bash
docker compose up -d --build
```

Перевірка стану:

```bash
docker compose ps
```

### 3) Адреси сервісів

- Swagger UI: `http://localhost:8080/swagger/index.html`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`
- pgAdmin: `http://localhost:5050`

### 4) Підключення pgAdmin до PostgreSQL

У pgAdmin (Create Server):
- Host: `db`
- Port: `5432`
- Database: значення `POSTGRES_DB` із локального `.env`
- Username: значення `POSTGRES_USER` із локального `.env`
- Password: значення `POSTGRES_PASSWORD` із локального `.env`

### 5) Міграції PostgreSQL

У контейнерному режимі API застосовує PostgreSQL-міграції на старті (`Database:ApplyMigrationsOnStartup=true`).
Помилка міграції зупиняє запуск API.

### 6) Build і тести

```bash
dotnet build
dotnet test
```

### 7) Керування сервісами

```bash
docker compose stop
docker compose start
docker compose restart
docker compose down
```

`docker compose down -v` видаляє named volumes і дані БД. Для перевірки персистентності в ЛР3 використовуйте `down` **без** `-v`.

### 8) Де зберігаються дані

- PostgreSQL: named volume `postgres_data`
- pgAdmin: named volume `pgadmin_data`

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

## Типові проблеми

- API/pgAdmin перезапускаються: перевірте `docker compose logs api` / `docker compose logs pgadmin`.
- Swagger недоступний: перевірте `docker compose ps` і порт `API_PORT`.
- Тести падають по датах планів ТО: використовуйте відносні майбутні дати в тестах, не фіксовані календарні дати.
