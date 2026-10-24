# EquipmentMonitoring

## Призначення
**EquipmentMonitoring** — навчальний Web API-проєкт системи моніторингу обладнання на .NET 10.

## Поточна реалізована функціональність (EPC-4, EPC-5)
- Реєстрація обладнання: `POST /api/equipment`.
- Отримання переліку обладнання: `GET /api/equipment`.
- Фільтрація за станом: `GET /api/equipment?status=Working`.
- Додавання вимірювання: `POST /api/equipment/{equipmentId}/measurements`.
- Історія вимірювань: `GET /api/equipment/{equipmentId}/measurements`.
- Фільтрація історії за типом та проміжком часу (`type`, `from`, `to`).
- Валідація вхідних даних `CreateEquipmentDto`.
- Валідація вхідних даних `CreateMeasurementDto` (обов’язкові Type/Value/Unit/MeasuredAt, finite-числа, перевірка одиниць).
- Перевірка унікальності `SerialNumber` (попередня перевірка + обробка конфлікту унікальності під час збереження).
- Збереження даних у SQLite через EF Core міграції.
- OpenAPI-документ та Swagger UI для документації API.

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
- **.NET 10 SDK**
- **Visual Studio** з підтримкою **.NET 10** та **ASP.NET**

## Команди запуску (з кореня репозиторію)
```bash
dotnet restore
dotnet tool restore
dotnet ef database update --project EquipmentMonitoring.Infrastructure --startup-project EquipmentMonitoring.API
dotnet run --project EquipmentMonitoring.API --launch-profile https
dotnet test
```

## Документація API
Після запуску візьміть фактичну адресу API з консолі (наприклад, `https://localhost:7114`).

- Swagger UI: `<BASE_URL>/swagger`
- OpenAPI JSON: `<BASE_URL>/openapi/v1.json`

## Приклади HTTP-запитів
Готові приклади запитів розміщені у файлі:

- `EquipmentMonitoring.API/EquipmentMonitoring.API.http`

## Jira
- EPC-4: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-4
- EPC-5: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-5
- EPC-33: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33
