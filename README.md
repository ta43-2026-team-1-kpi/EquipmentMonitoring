# EquipmentMonitoring

## Призначення
**EquipmentMonitoring** — навчальний Web API-проєкт системи моніторингу обладнання на .NET 10.

## Поточна реалізована функціональність (EPC-4, EPC-5, EPC-27–EPC-32)
- Реєстрація обладнання: `POST /api/equipment`.
- Отримання переліку обладнання: `GET /api/equipment`.
- Фільтрація за станом: `GET /api/equipment?status=Working`.
- Додавання вимірювання: `POST /api/equipment/{equipmentId}/measurements`.
- Історія вимірювань: `GET /api/equipment/{equipmentId}/measurements`.
- Фільтрація історії за типом та проміжком часу (`type`, `from`, `to`).
- Створення плану технічного обслуговування: `POST /api/maintenance-plans`.
- Перегляд планів, відсортованих за датою: `GET /api/maintenance-plans`; підтримується фільтр за `equipmentId`.
- Зміна стану плану з `Planned` на `Completed` або `Cancelled`: `PATCH /api/maintenance-plans/{maintenancePlanId}/status`.
- Валідація вхідних даних `CreateEquipmentDto`.
- Валідація вхідних даних `CreateMeasurementDto` (обов’язкові Type/Value/Unit/MeasuredAt, finite-числа, перевірка одиниць).
- Валідація планів обслуговування та перевірка, що обладнання зареєстроване.
- Перевірка унікальності `SerialNumber` (попередня перевірка + обробка конфлікту унікальності під час збереження).
- Збереження даних у SQLite через EF Core міграції.
- OpenAPI-документ та Swagger UI для документації API.

## Планування технічного обслуговування

Для створення плану передайте ідентифікатор зареєстрованого обладнання, заплановану дату у форматі `yyyy-MM-dd`, вид робіт, відповідального працівника та необов’язкову примітку. Новий запис автоматично отримує стан `Planned`.

`GET /api/maintenance-plans` повертає всі плани у порядку зростання `plannedDate`. Щоб переглянути плани лише для одного пристрою, додайте `?equipmentId=<GUID>`.

Після виконання або скасування робіт надішліть `PATCH /api/maintenance-plans/{maintenancePlanId}/status` з тілом `{"status":"Completed"}` або `{"status":"Cancelled"}`. Перехід дозволений тільки зі стану `Planned`; повторна зміна завершеного чи скасованого плану повертає `409 Conflict`.

## Домовленість одиниць вимірювання
- `Temperature` → `°C`
- `Voltage` → `V`
- `Load` → `%`

## Формат дати й часу для вимірювань
- Поле `measuredAt` приймається тільки у форматі ISO 8601 з `Z` або явним зсувом (`+02:00`, `-05:00`).
- Дата без зсуву (наприклад, `2026-10-24T10:30:00`) відхиляється як `400`.
- У БД час зберігається в UTC, у відповіді повертається `measuredAtUtc`.

## Структура рішення
- **EquipmentMonitoring.API** — HTTP-рівень, контролери, OpenAPI/Swagger, конфігурація DI.
- **EquipmentMonitoring.Domain** — сутності та правила предметної області.
- **EquipmentMonitoring.Application** — DTO, валідатори, прикладні контракти та винятки.
- **EquipmentMonitoring.Infrastructure** — EF Core, SQLite, репозиторії, міграції.
- **EquipmentMonitoring.Tests** — модульні та інтеграційні тести.

## Передумови
- **Git**
- **.NET 10 SDK**
- **Visual Studio** з підтримкою **.NET 10** та **ASP.NET**

## Запуск проєкту з нуля

1. Клонувати репозиторій:
```bash
git clone https://github.com/ta43-2026-team-1-kpi/EquipmentMonitoring.git
cd EquipmentMonitoring
```

2. Відкрити `EquipmentMonitoring.slnx` у Visual Studio.

3. Виконати команди з кореня репозиторію в такій послідовності:
```bash
dotnet restore
dotnet tool restore
dotnet build
dotnet test
dotnet ef database update --project EquipmentMonitoring.Infrastructure --startup-project EquipmentMonitoring.API
dotnet run --project EquipmentMonitoring.API --launch-profile https
```

`dotnet run` займає поточний термінал. Для зупинки API натисніть `Ctrl+C`.

Якщо є проблема довіри до локального HTTPS-сертифіката:
```bash
dotnet dev-certs https --trust
```

SQLite не потребує окремого сервера: під час міграцій створюється власна порожня БД у папці `EquipmentMonitoring.API`.
Після отримання нових міграцій потрібно повторити `dotnet ef database update`.

## Документація API
Після запуску перевіряйте фактичні адреси API в консолі.

- Swagger UI (типова адреса): `https://localhost:7114/swagger`
- OpenAPI JSON: `<BASE_URL>/openapi/v1.json`

## Приклади HTTP-запитів
Готові приклади запитів розміщені у файлі:

- `EquipmentMonitoring.API/EquipmentMonitoring.API.http`

Послідовність для EPC-5:
1. Спочатку створіть обладнання через `POST /api/equipment`.
2. Скопіюйте `id` зі відповіді.
3. У `.http` файлі замініть `@EquipmentId` на реальний `id`.

Для EPC-27–EPC-32 спершу зареєструйте обладнання та скопіюйте його `id` у `@EquipmentId`. Створіть план через `POST /api/maintenance-plans`, скопіюйте повернений `id` у `@MaintenancePlanId`, а потім перевірте список і зміну стану прикладами з `.http` файлу.

Для вимірювань використовуйте узгоджені одиниці:
- `Temperature` → `°C`
- `Voltage` → `V`
- `Load` → `%`

Час передавайте у форматі `yyyy-MM-ddTHH:mm:ss[.fffffff](Z|±HH:mm)`.
Підтримуються UTC (`Z`) та явні часові зсуви (`+02:00`, `-05:00`).

## Jira
- EPC-4: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-4
- EPC-5: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-5
- EPC-33: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33
- EPC-27: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-27
- EPC-28: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-28
- EPC-29: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-29
- EPC-30: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-30
- EPC-31: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-31
- EPC-32: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-32
