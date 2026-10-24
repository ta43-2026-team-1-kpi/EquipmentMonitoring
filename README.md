# EquipmentMonitoring

## Призначення
**EquipmentMonitoring** — це навчальний Web API-проєкт системи моніторингу обладнання на платформі .NET 10.

## Поточний стан
Наразі в рішенні створено початкову структуру проєктів за шарами архітектури.
Функції моніторингу обладнання ще не реалізовані.

## Структура рішення
- **EquipmentMonitoring.API** — HTTP-рівень (ендпоїнти, конфігурація застосунку, запуск Web API).
- **EquipmentMonitoring.Domain** — доменна модель і базові бізнес-сутності.
- **EquipmentMonitoring.Application** — сценарії застосунку та прикладна логіка між API і доменом.
- **EquipmentMonitoring.Infrastructure** — технічні реалізації (інтеграції, доступ до даних, зовнішні залежності).
- **EquipmentMonitoring.Tests** — автотести для перевірки поведінки рішень.

## Передумови
Для запуску потрібні:
- **.NET 10 SDK**
- **Visual Studio** з підтримкою **.NET 10** та **ASP.NET**

## Запуск і перевірка (з кореня репозиторію)
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project EquipmentMonitoring.API --launch-profile https
```

## Перевірка API
Після запуску фактичну адресу API потрібно взяти з консолі запуску.
Для базової перевірки наразі доступний демонстраційний ендпоїнт:
- `GET /weatherforecast`

## Jira
Завдання: **EPC-33**  
https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33
