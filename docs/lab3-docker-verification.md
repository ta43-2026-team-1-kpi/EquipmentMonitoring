# ЛР3 — Docker + PostgreSQL: фактична верифікація

## Вимога викладача → реалізація → перевірка

| Вимога | Реалізація | Фактична перевірка |
|---|---|---|
| 3 сервіси: API + PostgreSQL + pgAdmin | `docker-compose.yaml` (services: `api`, `db`, `pgadmin`) | `docker compose ps` показує 3 запущені сервіси |
| Dockerfile для API | `Dockerfile` (multi-stage, .NET 10 SDK/runtime) | `docker compose up -d --build` успішно збирає image API |
| Мережева взаємодія | explicit network `monitoring_net`, DB host `db` | API підключається до PostgreSQL, міграції застосовуються |
| Збереження PostgreSQL через volume | named volume `postgres_data` | Дані збережені після `restart` і `down/up` без `-v` |
| Параметри/паролі через `.env` | `.env.example` + локальний `.env`, env у compose | сервіси стартують із параметрами з `.env` |
| Доступ до Swagger/OpenAPI і pgAdmin | порти `127.0.0.1:${API_PORT}:8080`, `127.0.0.1:${PGADMIN_PORT}:80` | `openapi:200`, `swagger:200`, `pgadmin:302` |
| Перевірка функцій застосунку | реальні HTTP-запити до контейнерного API | див. розділ «Функціональні перевірки» |
| Перевірка персистентності | `restart`, `down`, `up -d` без `-v` | ті самі ID та значення знайдено повторно |

## Використані версії

- PostgreSQL image: `postgres:17.2`
- pgAdmin image: `dpage/pgadmin4:9.8`
- Npgsql EF Core provider: `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3`

## Команди та результати

### 1) Підйом стеку

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

Результат: стек валідний і запущений.

### 2) Доступність веб-інтерфейсів

```bash
curl -s -o NUL -w "openapi:%{http_code}" http://localhost:8080/openapi/v1.json
curl -s -o NUL -w "swagger:%{http_code}" http://localhost:8080/swagger/index.html
curl -s -o NUL -w "pgadmin:%{http_code}" http://localhost:5050/
```

Фактичний результат:
- `openapi:200`
- `swagger:200`
- `pgadmin:302` (редирект на login)

### 3) Функціональні HTTP-перевірки (контейнерний API)

Використано сценарій `scripts/lab3-functional-check.ps1`.

Фактичні статуси:
- Create equipment: `201`
- Serial trim rule: вхід `"  SN-LAB3-D0666441  "`, збережено/повернуто `"SN-LAB3-D0666441"`
- Duplicate serial: `400`
- List equipment: `200`
- Filter by status: `200` (усі записи у відповіді мають `Working`)
- Upsert global temperature threshold `0..80`: `200`
- Post measurement `50°C`: `201`
- Post measurement `90°C`: `201`
- History with filters: `200` (2 записи)
- Warnings list: `200`
- Warnings summary: `200`
- Acknowledge warning: `200` (`Acknowledged`)
- Create maintenance plan: `201`
- Update maintenance plan status: `200` (`Planned` -> `Completed`)

Демо-ID:
- EquipmentId: `2473a102-dc58-4638-86cc-a66b0d306a63`
- Measurement1Id: `f7f33a78-6f0b-4b7f-a4cf-fbe4e97890f3`
- Measurement2Id: `5d9eae2a-bc59-4b4f-af9d-feb68ac7b323`
- WarningId: `76f65b13-e237-4ddf-ba41-8816ad5be9fc`
- MaintenancePlanId: `6c5696fb-b9e7-4ae3-b290-d3056f0f50c6`

### 4) Пряма SQL-перевірка в PostgreSQL

Використано `scripts/lab3-postgres-check.ps1`.

Факт:
- запис обладнання знайдено з тим самим `Id` і `SerialNumber`;
- обидва вимірювання знайдено з тими самими `Id` і очікуваними значеннями (`50`, `90`);
- warning знайдено за зв'язкою (`WarningId` + `EquipmentId` + `MeasurementId`), статус `Acknowledged`, `AcknowledgedAtUtc` не `NULL`;
- maintenance plan знайдено, статус `Completed`.

Примітка: перший запуск SQL-скрипта впав через синтаксис `DO $$` після інтерполяції PowerShell; після виправлення delimiter-ів перевірка завершилась `lab3-postgres-check: OK`.

### 5) Перевірка збереження тих самих записів

Використано `scripts/lab3-persistence-check.ps1`:
1. перевірка до перезапуску,
2. `docker compose restart`,
3. `docker compose down` + `docker compose up -d` (без `-v`).

Результат:
- у всіх 3 фазах знайдено ті самі `EquipmentId`, `MeasurementId`, `WarningId`, `MaintenancePlanId`;
- значення/статуси збережені (`SerialNumber`, `WarningStatus`, `PlanStatus`).

## Build і тести

```bash
dotnet build
dotnet test
```

Фінальний результат:
- Build: успішно
- Tests: `325 passed, 0 failed, 0 skipped`

Додатково: після змін у `Program.cs`/`appsettings.json` тестові фабрики отримали явний тестовий `ConnectionStrings:EquipmentDatabase` (in-memory config + host setting), без використання реальних секретів.

## 10 початкових падінь MaintenancePlans: причина та виправлення

Початково було 10 падінь:
1. `CreateMaintenancePlanValidatorTests.Validate_WithValidDto_ShouldReturnValidResult`
2. `MaintenancePlansHttpIntegrationTests.Post_WithValidPlan_ShouldCreatePlannedRecord`
3. `MaintenancePlansHttpIntegrationTests.Post_ForUnregisteredEquipment_ShouldReturn404`
4. `MaintenancePlansHttpIntegrationTests.Get_ShouldReturnAllPlansSortedByDate`
5. `MaintenancePlansHttpIntegrationTests.Get_WithEquipmentFilter_ShouldReturnOnlyThatEquipmentPlans`
6. `MaintenancePlansHttpIntegrationTests.PatchStatus_FromPlanned_ShouldSetRequestedTerminalStatus(status: "Completed")`
7. `MaintenancePlansHttpIntegrationTests.PatchStatus_FromPlanned_ShouldSetRequestedTerminalStatus(status: "Cancelled")`
8. `MaintenancePlansHttpIntegrationTests.PatchStatus_ToPlanned_ShouldReturn400`
9. `MaintenancePlansHttpIntegrationTests.PatchStatus_WhenPlanIsAlreadyCompleted_ShouldReturn409`
10. `MaintenancePlansHttpIntegrationTests.PatchStatus_WhenPlanIsAlreadyCancelled_ShouldReturn409`

Підтверджена причина (гіпотеза про дати):
- так, падіння були спричинені фіксованими датами `2026-...`, які стали «минулим» відносно поточної дати;
- валідатор `CreateMaintenancePlanValidator` очікує `PlannedDate >= DateOnly.FromDateTime(DateTime.UtcNow)`.

Виправлення:
- у тестах замінено фіксовані дати на відносні майбутні (`UtcNow.AddDays(...)`), без зміни бізнес-правил.

Вплив на ЛР3:
- це не дефект Docker/PostgreSQL інтеграції;
- це деградація тестових даних у часі;
- після виправлення тести стабільні.

## Що перевірено автоматично / що вручну

Автоматично:
- compose підйом, доступність endpoint-ів, функціональні HTTP сценарії,
- SQL-перевірки в PostgreSQL,
- персистентність після `restart` і `down/up` без `-v`,
- build/test.

Вручну (користувачем у браузері):
- login у pgAdmin і візуальний перегляд таблиць через UI.

## Інструкція для входу в pgAdmin без друку пароля

1. Відкрийте локальний файл `.env` у корені репозиторію.
2. Візьміть значення `PGADMIN_DEFAULT_EMAIL` і `PGADMIN_DEFAULT_PASSWORD`.
3. Відкрийте `http://localhost:5050` і увійдіть.
4. Підключіть сервер `db:5432` із параметрами БД/користувача з того самого `.env`.
