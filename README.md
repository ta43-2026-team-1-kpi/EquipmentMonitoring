# EquipmentMonitoring

## Призначення

**EquipmentMonitoring** — навчальний ASP.NET Core Web API-проєкт для моніторингу обладнання.

У поточній реалізації доступні Story:
- **EPC-4** — реєстрація обладнання та перегляд переліку;
- **EPC-5** — додавання вимірювань і перегляд історії;
- **EPC-6** — межі (thresholds) та попередження (warnings);
- **EPC-7** — плани технічного обслуговування.

## Структура рішення та технології

Рішення містить 5 проєктів:

- **EquipmentMonitoring.API** — HTTP-рівень (контролери, маршрути, Swagger/OpenAPI, DI).
- **EquipmentMonitoring.Domain** — доменні сутності та enum.
- **EquipmentMonitoring.Application** — DTO, валідація, прикладні сервіси, контракти, прикладні винятки.
- **EquipmentMonitoring.Infrastructure** — EF Core, SQLite, конфігурації сутностей, репозиторії, міграції.
- **EquipmentMonitoring.Tests** — unit та integration тести (xUnit).

Використані технології:
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + SQLite
- OpenAPI (`MapOpenApi`) + Swagger UI
- xUnit
- інструмент міграцій: `dotnet-ef` (tool manifest `dotnet-tools.json`)

## Передумови

- Git
- .NET 10 SDK
- Visual Studio з підтримкою .NET 10 та ASP.NET

## Запуск після клонування (з нуля)

Усі команди виконуйте **з кореня репозиторію**.

1. Клонування і перехід у каталог:

```bash
git clone https://github.com/ta43-2026-team-1-kpi/EquipmentMonitoring.git
cd EquipmentMonitoring
```

2. Відкрийте `EquipmentMonitoring.slnx` у Visual Studio.

3. Виконайте команди:

```bash
dotnet restore
dotnet tool restore
dotnet build
dotnet test
dotnet ef database update --project EquipmentMonitoring.Infrastructure --startup-project EquipmentMonitoring.API
dotnet run --project EquipmentMonitoring.API --launch-profile https
```

`dotnet run` займає поточний термінал. Зупинка API: `Ctrl+C`.

## Оновлення існуючої копії

1. Зупиніть запущений API (`Ctrl+C`).
2. Оновіть `master`:

```bash
git checkout master
git pull origin master
```

3. Відновіть залежності та інструменти:

```bash
dotnet restore
dotnet tool restore
```

4. Застосуйте міграції:

```bash
dotnet ef database update --project EquipmentMonitoring.Infrastructure --startup-project EquipmentMonitoring.API
```

5. Перевірте тести та запустіть API:

```bash
dotnet test
dotnet run --project EquipmentMonitoring.API --launch-profile https
```

## Swagger і OpenAPI

З `launchSettings.json` (профіль `https`) типові адреси:
- `https://localhost:7114`
- `http://localhost:5190`

Документація:
- Swagger UI: `https://localhost:7114/swagger`
- OpenAPI JSON: `https://localhost:7114/openapi/v1.json`

Фактичний URL завжди перевіряйте в консолі під час запуску.

Якщо є проблема довіри до HTTPS-сертифіката:

```bash
dotnet dev-certs https --trust
```

## SQLite база даних

- Файл БД: `EquipmentMonitoring.API/equipment-monitoring.db`.
- Окремий сервер БД не потрібен.
- Файл БД не передається через Git.
- Перше `dotnet ef database update` створює БД, наступні застосовують нові міграції зі збереженням даних відповідно до логіки міграцій.

Для перегляду БД можна використовувати **DB Browser for SQLite**.

## Endpoint-и (EPC-4 — EPC-7)

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
- `GET /api/warnings/{warningId}`
- `POST /api/warnings/{warningId}/acknowledge`

### EPC-7 (плани ТО)
- `POST /api/maintenance-plans`
- `GET /api/maintenance-plans`
- `PATCH /api/maintenance-plans/{maintenancePlanId}/status`

## Приклади послідовності роботи

1. **Обладнання → вимірювання**
   1) Створіть обладнання через `POST /api/equipment`.
   2) Скопіюйте `id` з відповіді.
   3) Додайте вимірювання через `POST /api/equipment/{equipmentId}/measurements`.

2. **Межа → вимірювання поза межею → попередження**
   1) Задайте межу (`PUT /api/thresholds/{measurementType}` або `PUT /api/equipment/{equipmentId}/thresholds/{measurementType}`).
   2) Додайте вимірювання поза межами.
   3) Перегляньте попередження через `GET /api/warnings`.

3. **Створення плану → зміна стану**
   1) Створіть план через `POST /api/maintenance-plans`.
   2) Змініть стан через `PATCH /api/maintenance-plans/{maintenancePlanId}/status`.

Готові приклади запитів: `EquipmentMonitoring.API/EquipmentMonitoring.API.http`.
Для сценаріїв вимірювань у `.http` замініть `@EquipmentId` на реальний `id` обладнання.

## Правила одиниць, часу та фільтрів

- Одиниці вимірювання:
  - `Temperature` → `°C`
  - `Voltage` → `V`
  - `Load` → `%`
- Для вимірювань час передається у форматі `yyyy-MM-ddTHH:mm:ss[.fffffff](Z|±HH:mm)`.
- `measuredAt`, `from`, `to` приймаються лише із `Z` або явним зсувом; значення нормалізуються до UTC.
- У `GET /api/equipment/{equipmentId}/measurements` фільтри `type`, `from`, `to` необов’язкові, комбінуються через AND, межі часу включні.

## Типові проблеми

- **DLL заблоковані запущеним API**: зупиніть `dotnet run` (`Ctrl+C`) і повторіть `build/test`.
- **`no such table ...`**: не застосовані міграції; виконайте `dotnet ef database update --project EquipmentMonitoring.Infrastructure --startup-project EquipmentMonitoring.API`.
- **`localhost` недоступний**: API не запущено або використовується інший фактичний URL (перевірте консоль).

## Jira

- EPC-4: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-4
- EPC-5: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-5
- EPC-6: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-6
- EPC-7: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-7
- EPC-33: https://ta43-2026-team-1-kpi.atlassian.net/browse/EPC-33

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
