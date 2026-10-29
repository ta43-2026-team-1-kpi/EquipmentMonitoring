# EPC-6: зведення активних попереджень — цикл TDD

Ендпоінт `GET /api/warnings/summary` реалізовано за TDD. Кожен етап — окремий коміт у гілці `feature/EPC-34-warning-summary`.

## Вимоги

- загальна кількість активних попереджень;
- для кожного обладнання з активними попередженнями: назва, серійний номер, кількість, час останнього, розбивка за типами показників;
- опрацьовані (Acknowledged) попередження не враховуються;
- порядок: спочатку обладнання з найбільшою кількістю, за рівної кількості — з новішим попередженням;
- схема БД і наявні ендпоінти не змінюються.

Стартовий стан: 313 тестів, усі зелені.

## Red

Коміт `EPC-34: add failing tests for active warnings summary`.

Тести на всіх рівнях, плюс мінімальний каркас (DTO, метод репозиторію, метод сервісу з `NotImplementedException`), щоб проєкт компілювався:

| Рівень | Тест | Причина падіння |
|---|---|---|
| Infrastructure | `CountActiveByEquipmentAndTypeAsync_ShouldGroupByEquipmentAndMeasurementType` | `NotImplementedException` |
| Infrastructure | `CountActiveByEquipmentAndTypeAsync_ShouldIgnoreAcknowledgedWarnings` | `NotImplementedException` |
| Application | `GetActiveSummaryAsync_ShouldGroupByEquipmentWithMostAffectedFirst` | `NotImplementedException` |
| Application | `GetActiveSummaryAsync_WithEqualCounts_ShouldPutMostRecentFirst` | `NotImplementedException` |
| Application | `GetActiveSummaryAsync_WithoutActiveWarnings_ShouldReturnEmptySummary` | `NotImplementedException` |
| API | `GetSummary_ShouldCountActiveWarningsPerEquipment` | очікувався 200, отримано 404 |
| API | `GetSummary_WithoutWarnings_ShouldReturnEmptySummary` | очікувався 200, отримано 404 |
| OpenAPI | операція описана, є приклад відповіді, поля схем описані (5 тестів) | операції та схем немає в документі |

Результат: 325 тестів, 313 пройшли, 12 впали — усі нові, і саме через відсутню функціональність.

## Green

Коміт `EPC-34: implement active warnings summary`.

- `WarningRepository.CountActiveByEquipmentAndTypeAsync` — найпростіший варіант: завантажує активні попередження і групує їх у пам'яті;
- `MonitoringMappings.ToSummaryDto` — групування за обладнанням і сортування;
- `WarningService.GetActiveSummaryAsync`, `GET /api/warnings/summary`, приклад у Swagger.

Результат: 325 з 325, нових попереджень компілятора немає.

## Refactor

Коміт `EPC-34: aggregate active warnings in the database`.

Green-версія тягнула з бази всі активні попередження разом з обладнанням лише для того, щоб їх порахувати. Агрегацію перенесено в один SQL-запит (`GROUP BY` обладнання і тип, `COUNT`, `MAX`), тож з бази повертається по одному рядку на пару «обладнання — тип».

Поведінка не змінилась: 325 з 325, тести не правились.
