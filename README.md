# EquipmentMonitoring

## Призначення

**EquipmentMonitoring** — навчальний Web API-проєкт системи моніторингу обладнання на .NET 10.

## EPC-4 — Реєстрація обладнання

- Реєстрація обладнання: `POST /api/equipment`.
- Отримання переліку обладнання: `GET /api/equipment`.
- Фільтрація обладнання за станом: `GET /api/equipment?status=Working`.
- Валідація вхідних даних `CreateEquipmentDto`.
- Перевірка унікальності `SerialNumber`: попередня перевірка та обробка конфлікту унікальності під час збереження.

## EPC-5 — Вимірювання обладнання

- Додавання вимірювання: `POST /api/equipment/{equipmentId}/measurements`.
- Перегляд історії вимірювань: `GET /api/equipment/{equipmentId}/measurements`.
- Фільтрація історії за типом і проміжком часу (`type`, `from`, `to`).
- Валідація `CreateMeasurementDto`: обов’язкові Type/Value/Unit/MeasuredAt, перевірка скінченності чисел і відповідності одиниць.
- Час приймається в ISO 8601 лише з `Z` або явним часовим зсувом; у БД зберігається UTC, у відповіді повертається `measuredAtUtc`.

### Домовленість одиниць вимірювання

- `Temperature` → `°C`
- `Voltage` → `V`
- `Load` → `%`

### Формат дати й часу для вимірювань

- Поле `measuredAt` приймається тільки у форматі ISO 8601 з `Z` або явним зсувом (`+02:00`, `-05:00`).
- Дата без зсуву (наприклад, `2026-10-24T10:30:00`) відхиляється як `400`.
- У БД час зберігається в UTC, у відповіді повертається `measuredAtUtc`.

## EPC-6 — Контроль допустимих значень і попередження

Кожне нове вимірювання автоматично порівнюється з допустимими межами. Якщо значення виходить за межу, створюється попередження. Вимірювання і попередження зберігаються в одній транзакції; відповідь `POST .../measurements` не змінилася.

Межі задаються на двох рівнях:
- **глобальна** — для типу показника, діє для всього обладнання: `/api/thresholds/{measurementType}`;
- **для обладнання** — перекриває глобальну для цього обладнання, навіть якщо м'якша: `/api/equipment/{equipmentId}/thresholds/{measurementType}`.

Правила:
- потрібна хоча б одна межа (`minValue` або `maxValue`), `minValue ≤ maxValue`;
- межі включні: значення, що дорівнює межі, попередження не створює;
- межі задаються в канонічній одиниці типу (див. нижче);
- попередження зберігає значення межі на момент створення, тому зміна чи видалення межі не змінює наявні попередження.

| Метод | Шлях | Призначення |
|---|---|---|
| `GET` | `/api/thresholds` | глобальні межі |
| `GET` / `PUT` / `DELETE` | `/api/thresholds/{measurementType}` | глобальна межа типу; `PUT` повертає `201` при створенні, `200` при оновленні |
| `GET` | `/api/equipment/{equipmentId}/thresholds` | власні межі обладнання |
| `GET` / `PUT` / `DELETE` | `/api/equipment/{equipmentId}/thresholds/{measurementType}` | власна межа обладнання |
| `GET` | `/api/equipment/{equipmentId}/thresholds/effective` | межі, за якими зараз перевіряється обладнання |
| `GET` | `/api/warnings` | попередження; без параметрів — активні, новіші спочатку |
| `GET` | `/api/warnings/{warningId}` | попередження за id |
| `POST` | `/api/warnings/{warningId}/acknowledge` | позначити опрацьованим (ідемпотентно) |

Параметри `GET /api/warnings`: `equipmentId`, `status` (`Active` за замовчуванням, `Acknowledged`, `All`), `measurementType`, `page` (від 1), `pageSize` (1–200, за замовчуванням 50). Відповідь містить `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

Після оновлення з цією функціональністю застосуйте міграцію `AddThresholdsAndWarnings` командою `dotnet ef database update` (див. нижче).

## EPC-7 — Планування технічного обслуговування

Для створення плану передайте ідентифікатор зареєстрованого обладнання, заплановану дату у форматі `yyyy-MM-dd`, вид робіт, відповідального працівника та необов’язкову примітку. Новий запис автоматично отримує стан `Planned`.

`GET /api/maintenance-plans` повертає всі плани у порядку зростання `plannedDate`. Щоб переглянути плани лише для одного пристрою, додайте `?equipmentId=<GUID>`.

Після виконання або скасування робіт надішліть `PATCH /api/maintenance-plans/{maintenancePlanId}/status` з тілом `{"status":"Completed"}` або `{"status":"Cancelled"}`. Перехід дозволений тільки зі стану `Planned`; повторна зміна завершеного чи скасованого плану повертає `409 Conflict`.

## Спільні технічні можливості

- Збереження даних у SQLite через міграції EF Core.
- OpenAPI-документ і Swagger UI для документації API.

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
- EPC-6: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-6
- EPC-7: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-7
- EPC-33: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33
- EPC-27: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-27
- EPC-28: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-28
- EPC-29: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-29
- EPC-30: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-30
- EPC-31: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-31
- EPC-32: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-32
