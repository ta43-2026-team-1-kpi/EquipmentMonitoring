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
