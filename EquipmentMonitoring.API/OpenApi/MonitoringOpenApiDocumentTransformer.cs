using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EquipmentMonitoring.API.OpenApi
{
    /// <summary>
    /// Adds request/response examples to the thresholds and warnings endpoints.
    /// Summaries, descriptions and schemas come from controller and DTO attributes.
    /// </summary>
    public sealed class MonitoringOpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        private const string GlobalThreshold = "/api/thresholds/{measurementType}";
        private const string GlobalThresholds = "/api/thresholds";
        private const string EquipmentThreshold = "/api/equipment/{equipmentId}/thresholds/{measurementType}";
        private const string EquipmentThresholds = "/api/equipment/{equipmentId}/thresholds";
        private const string EffectiveThresholds = "/api/equipment/{equipmentId}/thresholds/effective";
        private const string Warnings = "/api/warnings";
        private const string WarningSummary = "/api/warnings/summary";
        private const string WarningById = "/api/warnings/{warningId}";
        private const string AcknowledgeWarning = "/api/warnings/{warningId}/acknowledge";

        private static readonly ResponseExample[] ResponseExamples =
        [
            new(GlobalThresholds, HttpMethod.Get, "200", "Глобальні межі (масив може бути порожнім).", Examples.GlobalThresholdList),
            new(GlobalThreshold, HttpMethod.Get, "200", "Глобальна межа.", Examples.GlobalThreshold),
            new(GlobalThreshold, HttpMethod.Get, "400", "Невідомий тип показника.", Examples.InvalidMeasurementType),
            new(GlobalThreshold, HttpMethod.Get, "404", "Межу для типу показника не задано.", Examples.ThresholdNotFound),
            new(GlobalThreshold, HttpMethod.Put, "200", "Межу оновлено.", Examples.GlobalThreshold),
            new(GlobalThreshold, HttpMethod.Put, "201", "Межу створено. Заголовок Location містить її адресу.", Examples.GlobalThreshold),
            new(GlobalThreshold, HttpMethod.Put, "400", "Некоректні межі або тип показника.", Examples.InvalidLimits),
            new(GlobalThreshold, HttpMethod.Put, "409", "Межу щойно створено паралельним запитом; запит можна повторити.", Examples.ThresholdConflict),
            new(GlobalThreshold, HttpMethod.Delete, "400", "Невідомий тип показника.", Examples.InvalidMeasurementType),
            new(GlobalThreshold, HttpMethod.Delete, "404", "Межу для типу показника не задано.", Examples.ThresholdNotFound),

            new(EquipmentThresholds, HttpMethod.Get, "200", "Власні межі обладнання (масив може бути порожнім).", Examples.EquipmentThresholdList),
            new(EquipmentThresholds, HttpMethod.Get, "404", "Обладнання не знайдено.", Examples.EquipmentNotFound),
            new(EffectiveThresholds, HttpMethod.Get, "200", "Чинні межі за типами показників.", Examples.EffectiveThresholdList),
            new(EffectiveThresholds, HttpMethod.Get, "404", "Обладнання не знайдено.", Examples.EquipmentNotFound),
            new(EquipmentThreshold, HttpMethod.Get, "200", "Власна межа обладнання.", Examples.EquipmentThreshold),
            new(EquipmentThreshold, HttpMethod.Get, "400", "Невідомий тип показника.", Examples.InvalidMeasurementType),
            new(EquipmentThreshold, HttpMethod.Get, "404", "Обладнання не знайдено або межу не задано.", Examples.ThresholdNotFound),
            new(EquipmentThreshold, HttpMethod.Put, "200", "Межу оновлено.", Examples.EquipmentThreshold),
            new(EquipmentThreshold, HttpMethod.Put, "201", "Межу створено. Заголовок Location містить її адресу.", Examples.EquipmentThreshold),
            new(EquipmentThreshold, HttpMethod.Put, "400", "Некоректні межі або тип показника.", Examples.InvalidLimits),
            new(EquipmentThreshold, HttpMethod.Put, "404", "Обладнання не знайдено.", Examples.EquipmentNotFound),
            new(EquipmentThreshold, HttpMethod.Put, "409", "Межу щойно створено паралельним запитом; запит можна повторити.", Examples.ThresholdConflict),
            new(EquipmentThreshold, HttpMethod.Delete, "400", "Невідомий тип показника.", Examples.InvalidMeasurementType),
            new(EquipmentThreshold, HttpMethod.Delete, "404", "Обладнання не знайдено або межу не задано.", Examples.ThresholdNotFound),

            new(Warnings, HttpMethod.Get, "200", "Сторінка попереджень.", Examples.WarningPage),
            new(Warnings, HttpMethod.Get, "400", "Некоректні параметри фільтрації або пагінації.", Examples.InvalidWarningQuery),
            new(Warnings, HttpMethod.Get, "404", "Обладнання з фільтра equipmentId не знайдено.", Examples.EquipmentNotFound),
            new(WarningSummary, HttpMethod.Get, "200", "Зведення (без активних попереджень: totalActive = 0, equipment = []).", Examples.WarningSummary),
            new(WarningById, HttpMethod.Get, "200", "Попередження.", Examples.ActiveWarning),
            new(WarningById, HttpMethod.Get, "404", "Попередження не знайдено.", Examples.WarningNotFound),
            new(AcknowledgeWarning, HttpMethod.Post, "200", "Попередження опрацьовано (або вже було опрацьоване).", Examples.AcknowledgedWarning),
            new(AcknowledgeWarning, HttpMethod.Post, "404", "Попередження не знайдено.", Examples.WarningNotFound)
        ];

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            foreach (var example in ResponseExamples)
            {
                ApplyResponseExample(document, example);
            }

            ApplyRequestExample(document, GlobalThreshold, Examples.UpsertThresholdRequest);
            ApplyRequestExample(document, EquipmentThreshold, Examples.UpsertThresholdRequest);

            return Task.CompletedTask;
        }

        private static void ApplyResponseExample(OpenApiDocument document, ResponseExample example)
        {
            var operation = FindOperation(document, example.Path, example.Method);

            if (operation?.Responses is null || !operation.Responses.TryGetValue(example.StatusCode, out var response))
            {
                return;
            }

            var mediaType = example.StatusCode.StartsWith('2') ? "application/json" : "application/problem+json";

            operation.Responses[example.StatusCode] = new OpenApiResponse
            {
                Description = example.Description,
                Headers = response.Headers,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [mediaType] = new OpenApiMediaType
                    {
                        Schema = response.Content?.Values.FirstOrDefault()?.Schema,
                        Example = JsonNode.Parse(example.Json)
                    }
                }
            };
        }

        private static void ApplyRequestExample(OpenApiDocument document, string path, string json)
        {
            var operation = FindOperation(document, path, HttpMethod.Put);

            if (operation?.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) != true)
            {
                return;
            }

            operation.RequestBody.Content["application/json"] = new OpenApiMediaType
            {
                Schema = mediaType!.Schema,
                Example = JsonNode.Parse(json)
            };
        }

        private static OpenApiOperation? FindOperation(OpenApiDocument document, string path, HttpMethod method)
        {
            if (document.Paths is null || !document.Paths.TryGetValue(path, out var pathItem) || pathItem.Operations is null)
            {
                return null;
            }

            return pathItem.Operations.TryGetValue(method, out var operation) ? operation : null;
        }

        private sealed record ResponseExample(string Path, HttpMethod Method, string StatusCode, string Description, string Json);

        private static class Examples
        {
            public const string UpsertThresholdRequest = """
                { "minValue": -10, "maxValue": 80 }
                """;

            public const string GlobalThreshold = """
                {
                  "id": "3f2b7c1e-8a4d-4f5e-9b6a-1c2d3e4f5a6b",
                  "scope": "Global",
                  "equipmentId": null,
                  "measurementType": "Temperature",
                  "unit": "°C",
                  "minValue": -10,
                  "maxValue": 80,
                  "createdAtUtc": "2026-10-25T12:00:00+00:00",
                  "updatedAtUtc": "2026-10-25T12:00:00+00:00"
                }
                """;

            public const string EquipmentThreshold = """
                {
                  "id": "7a1c2e3f-4b5d-4e6f-8a9b-0c1d2e3f4a5b",
                  "scope": "Equipment",
                  "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "measurementType": "Load",
                  "unit": "%",
                  "minValue": null,
                  "maxValue": 70,
                  "createdAtUtc": "2026-10-25T12:05:00+00:00",
                  "updatedAtUtc": "2026-10-25T12:05:00+00:00"
                }
                """;

            public const string GlobalThresholdList = "[" + GlobalThreshold + "]";

            public const string EquipmentThresholdList = "[" + EquipmentThreshold + "]";

            public const string EffectiveThresholdList = "[" + GlobalThreshold + "," + EquipmentThreshold + "]";

            public const string ActiveWarning = """
                {
                  "id": "c4d5e6f7-a8b9-4c0d-9e1f-2a3b4c5d6e7f",
                  "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "equipmentName": "Compressor A1",
                  "equipmentSerialNumber": "SN-2026-0001",
                  "measurementId": "9d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "measurementType": "Temperature",
                  "actualValue": 92.5,
                  "unit": "°C",
                  "breachType": "AboveMaximum",
                  "limitValue": 80,
                  "thresholdId": "3f2b7c1e-8a4d-4f5e-9b6a-1c2d3e4f5a6b",
                  "thresholdScope": "Global",
                  "measuredAtUtc": "2026-10-25T11:58:00+00:00",
                  "createdAtUtc": "2026-10-25T11:58:01+00:00",
                  "status": "Active",
                  "acknowledgedAtUtc": null
                }
                """;

            public const string AcknowledgedWarning = """
                {
                  "id": "c4d5e6f7-a8b9-4c0d-9e1f-2a3b4c5d6e7f",
                  "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "equipmentName": "Compressor A1",
                  "equipmentSerialNumber": "SN-2026-0001",
                  "measurementId": "9d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "measurementType": "Temperature",
                  "actualValue": 92.5,
                  "unit": "°C",
                  "breachType": "AboveMaximum",
                  "limitValue": 80,
                  "thresholdId": "3f2b7c1e-8a4d-4f5e-9b6a-1c2d3e4f5a6b",
                  "thresholdScope": "Global",
                  "measuredAtUtc": "2026-10-25T11:58:00+00:00",
                  "createdAtUtc": "2026-10-25T11:58:01+00:00",
                  "status": "Acknowledged",
                  "acknowledgedAtUtc": "2026-10-25T12:10:00+00:00"
                }
                """;

            public const string WarningPage = """
                { "items": [
                """ + ActiveWarning + """
                ], "page": 1, "pageSize": 50, "totalCount": 1, "totalPages": 1 }
                """;

            public const string WarningSummary = """
                {
                  "totalActive": 4,
                  "equipment": [
                    {
                      "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                      "equipmentName": "Compressor A1",
                      "equipmentSerialNumber": "SN-2026-0001",
                      "activeCount": 3,
                      "latestCreatedAtUtc": "2026-10-25T11:58:01+00:00",
                      "byMeasurementType": [
                        { "measurementType": "Temperature", "count": 2 },
                        { "measurementType": "Load", "count": 1 }
                      ]
                    },
                    {
                      "equipmentId": "1b2c3d4e-5f60-4a7b-8c9d-0e1f2a3b4c5d",
                      "equipmentName": "Pump P-7",
                      "equipmentSerialNumber": "SN-2026-0002",
                      "activeCount": 1,
                      "latestCreatedAtUtc": "2026-10-25T11:40:12+00:00",
                      "byMeasurementType": [
                        { "measurementType": "Voltage", "count": 1 }
                      ]
                    }
                  ]
                }
                """;

            public const string InvalidMeasurementType = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "Помилка валідації запиту.",
                  "status": 400,
                  "errors": { "measurementType": ["Вкажіть один із допустимих типів: Temperature, Voltage, Load."] }
                }
                """;

            public const string InvalidLimits = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "Помилка валідації запиту.",
                  "status": 400,
                  "errors": {
                    "MinValue": ["Мінімальне значення не може перевищувати максимальне."],
                    "MaxValue": ["Максимальне значення не може бути меншим за мінімальне."]
                  }
                }
                """;

            public const string InvalidWarningQuery = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "Помилка валідації запиту.",
                  "status": 400,
                  "errors": {
                    "status": ["Вкажіть один із допустимих станів: Active, Acknowledged, All."],
                    "pageSize": ["Розмір сторінки має бути від 1 до 200."]
                  }
                }
                """;

            public const string ThresholdNotFound = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                  "title": "Межу не знайдено.",
                  "status": 404,
                  "errors": { "measurementType": ["Межу для показника Temperature не задано."] }
                }
                """;

            public const string EquipmentNotFound = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                  "title": "Обладнання не знайдено.",
                  "status": 404,
                  "errors": { "equipmentId": ["Обладнання з таким ідентифікатором не знайдено."] }
                }
                """;

            public const string WarningNotFound = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                  "title": "Попередження не знайдено.",
                  "status": 404,
                  "errors": { "warningId": ["Попередження з таким ідентифікатором не знайдено."] }
                }
                """;

            public const string ThresholdConflict = """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                  "title": "Конфлікт паралельного оновлення межі.",
                  "status": 409,
                  "errors": { "measurementType": ["Межу для показника Temperature щойно змінено іншим запитом. Повторіть запит."] }
                }
                """;
        }
    }
}
