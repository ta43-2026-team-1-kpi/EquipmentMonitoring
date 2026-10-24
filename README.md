# EquipmentMonitoring

## Призначення
**EquipmentMonitoring** — навчальний Web API-проєкт системи моніторингу обладнання на .NET 10.

## Поточна реалізована функціональність (EPC-4)
- Реєстрація обладнання: `POST /api/equipment`.
- Отримання переліку обладнання: `GET /api/equipment`.
- Фільтрація за станом: `GET /api/equipment?status=Working`.
- Валідація вхідних даних `CreateEquipmentDto`.
- Перевірка унікальності `SerialNumber` (попередня перевірка + обробка конфлікту унікальності під час збереження).
- Збереження даних у SQLite через EF Core міграції.
- OpenAPI-документ та Swagger UI для документації API.

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
- EPC-33: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33
