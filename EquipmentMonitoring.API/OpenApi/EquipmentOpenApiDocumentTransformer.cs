using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Application.DTOs.Measurement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace EquipmentMonitoring.API.OpenApi
{
    public sealed class EquipmentOpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            ConfigureSchemas(document);
            ConfigureEquipmentEndpoints(document);
            ConfigureMeasurementEndpoints(document);

            return Task.CompletedTask;
        }

        private static void ConfigureSchemas(OpenApiDocument document)
        {
            if (document.Components?.Schemas is null)
            {
                return;
            }

            if (document.Components.Schemas.TryGetValue(nameof(CreateEquipmentDto), out var createSchema))
            {
                createSchema.Description = "Запит на реєстрацію обладнання. Усі поля обов’язкові. Текстові поля: Name (до 200), Type (до 100), SerialNumber (до 100), Location (до 200).";
                ConfigureTextProperty(createSchema, nameof(CreateEquipmentDto.Name), "Назва обладнання. Обов’язкове поле, максимум 200 символів.");
                ConfigureTextProperty(createSchema, nameof(CreateEquipmentDto.Type), "Тип обладнання. Обов’язкове поле, максимум 100 символів.");
                ConfigureTextProperty(createSchema, nameof(CreateEquipmentDto.SerialNumber), "Серійний номер обладнання. Обов’язкове поле, максимум 100 символів.");
                ConfigureTextProperty(createSchema, nameof(CreateEquipmentDto.Location), "Місце встановлення обладнання. Обов’язкове поле, максимум 200 символів.");

                if (createSchema.Properties.TryGetValue(nameof(CreateEquipmentDto.Status), out var statusProperty))
                {
                    statusProperty.Description = "Стан обладнання. Обов’язкове поле. Допустимі значення: Working, Faulty, UnderMaintenance.";
                }
            }

            if (document.Components.Schemas.TryGetValue(nameof(EquipmentDto), out var equipmentSchema))
            {
                equipmentSchema.Description = "Дані зареєстрованого обладнання.";
                ConfigureTextProperty(equipmentSchema, nameof(EquipmentDto.Name), "Назва обладнання.");
                ConfigureTextProperty(equipmentSchema, nameof(EquipmentDto.Type), "Тип обладнання.");
                ConfigureTextProperty(equipmentSchema, nameof(EquipmentDto.SerialNumber), "Серійний номер обладнання.");
                ConfigureTextProperty(equipmentSchema, nameof(EquipmentDto.Location), "Місце встановлення обладнання.");

                if (equipmentSchema.Properties.TryGetValue(nameof(EquipmentDto.Id), out var idProperty))
                {
                    idProperty.Description = "Унікальний ідентифікатор обладнання (Guid), генерується системою.";
                }

                if (equipmentSchema.Properties.TryGetValue(nameof(EquipmentDto.Status), out var statusProperty))
                {
                    statusProperty.Description = "Поточний стан обладнання. Можливі значення: Working, Faulty, UnderMaintenance.";
                }
            }

            if (document.Components.Schemas.TryGetValue(nameof(CreateMeasurementDto), out var createMeasurementSchema))
            {
                createMeasurementSchema.Description = "Запит на додавання вимірювання. Обов’язкові Type, Value, Unit, MeasuredAt.";

                if (createMeasurementSchema.Properties.TryGetValue(nameof(CreateMeasurementDto.Type), out var typeProperty))
                {
                    typeProperty.Description = "Тип показника: Temperature, Voltage, Load.";
                }

                if (createMeasurementSchema.Properties.TryGetValue(nameof(CreateMeasurementDto.Value), out var valueProperty))
                {
                    valueProperty.Description = "Числове значення показника. Допускається 0 та від’ємна температура. Значення має бути скінченним числом.";
                }

                ConfigureTextProperty(createMeasurementSchema, nameof(CreateMeasurementDto.Unit), "Одиниця: Temperature → °C, Voltage → V, Load → % (точний збіг).");

                if (createMeasurementSchema.Properties.TryGetValue(nameof(CreateMeasurementDto.MeasuredAt), out var measuredAtProperty))
                {
                    measuredAtProperty.Description = "Дата й час ISO 8601 з Z або явним зсувом. Без зсуву — 400.";
                }
            }

            if (document.Components.Schemas.TryGetValue(nameof(MeasurementDto), out var measurementSchema))
            {
                measurementSchema.Description = "Дані вимірювання обладнання. Час нормалізований до UTC (+00:00).";

                if (measurementSchema.Properties.TryGetValue(nameof(MeasurementDto.Id), out var idProperty))
                {
                    idProperty.Description = "Унікальний ідентифікатор вимірювання (Guid).";
                }

                if (measurementSchema.Properties.TryGetValue(nameof(MeasurementDto.EquipmentId), out var equipmentIdProperty))
                {
                    equipmentIdProperty.Description = "Ідентифікатор обладнання.";
                }

                if (measurementSchema.Properties.TryGetValue(nameof(MeasurementDto.Type), out var typeProperty))
                {
                    typeProperty.Description = "Тип показника: Temperature, Voltage, Load.";
                }

                if (measurementSchema.Properties.TryGetValue(nameof(MeasurementDto.Value), out var valueProperty))
                {
                    valueProperty.Description = "Числове значення показника.";
                }

                ConfigureTextProperty(measurementSchema, nameof(MeasurementDto.Unit), "Одиниця вимірювання: °C, V або %.");

                if (measurementSchema.Properties.TryGetValue(nameof(MeasurementDto.MeasuredAtUtc), out var measuredAtProperty))
                {
                    measuredAtProperty.Description = "Дата й час вимірювання в UTC (ISO 8601, +00:00).";
                }
            }

            if (document.Components.Schemas.TryGetValue(nameof(ValidationProblemDetails), out var validationProblemSchema))
            {
                validationProblemSchema.Description = "Формат помилки валідації. Деталі по полях знаходяться в errors.";
            }
        }

        private static void ConfigureEquipmentEndpoints(OpenApiDocument document)
        {
            var path = document.Paths.FirstOrDefault(p => string.Equals(p.Key, "/api/Equipment", StringComparison.OrdinalIgnoreCase));

            if (path.Equals(default(KeyValuePair<string, IOpenApiPathItem>)))
            {
                return;
            }

            ConfigureEquipmentPost(path.Value);
            ConfigureEquipmentGet(path.Value);
        }

        private static void ConfigureMeasurementEndpoints(OpenApiDocument document)
        {
            var path = document.Paths.FirstOrDefault(p => string.Equals(p.Key, "/api/equipment/{equipmentId}/measurements", StringComparison.OrdinalIgnoreCase));

            if (path.Equals(default(KeyValuePair<string, IOpenApiPathItem>)))
            {
                return;
            }

            ConfigureMeasurementPost(path.Value);
            ConfigureMeasurementGet(path.Value);
        }

        private static void ConfigureEquipmentPost(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Post, out var post))
            {
                return;
            }

            post.Responses.Remove("200");
            post.Summary = "Реєстрація обладнання";
            post.Description = "Створює новий запис обладнання.";

            if (post.RequestBody?.Content.TryGetValue("application/json", out var requestJson) == true)
            {
                post.RequestBody.Content["application/json"] = new OpenApiMediaType
                {
                    Schema = requestJson.Schema,
                    Example = CreatePostRequestExample()
                };
            }

            post.Responses["201"] = CreateJsonResponse("Обладнання успішно зареєстровано.", GetJsonSchema(post.Responses, "201"), CreatePostSuccessExample());
            post.Responses["400"] = CreateJsonResponse("Некоректні вхідні дані або дубль серійного номера.", GetJsonSchema(post.Responses, "400"), CreateValidationErrorExample());
            post.Responses["409"] = CreateJsonResponse("Конфлікт унікальності серійного номера під час збереження.", GetJsonSchema(post.Responses, "409"), CreateConflictExample());
        }

        private static void ConfigureEquipmentGet(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Get, out var get))
            {
                return;
            }

            get.Summary = "Перелік обладнання та фільтрація за станом";
            get.Description = "Повертає перелік обладнання. Фільтр status необов’язковий.";

            var statusParameter = get.Parameters.FirstOrDefault(p => string.Equals(p.Name, "status", StringComparison.Ordinal));
            if (statusParameter is not null)
            {
                statusParameter.Description = "Необов’язковий фільтр status: Working, Faulty, UnderMaintenance.";
            }

            get.Responses["200"] = CreateJsonResponse("Успішно. Повертає масив EquipmentDto.", GetJsonSchema(get.Responses, "200"), CreateGetListExample());
            get.Responses["400"] = CreateJsonResponse("Некоректне значення параметра status.", GetJsonSchema(get.Responses, "400"), CreateStatusErrorExample());
        }

        private static void ConfigureMeasurementPost(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Post, out var post))
            {
                return;
            }

            post.Responses.Remove("200");
            post.Summary = "Додавання вимірювання для обладнання";
            post.Description = "Створює вимірювання для equipmentId. Час measuredAt має бути ISO 8601 з Z або явним зсувом і зберігається в UTC.";

            if (post.RequestBody?.Content.TryGetValue("application/json", out var requestJson) == true)
            {
                post.RequestBody.Content["application/json"] = new OpenApiMediaType
                {
                    Schema = requestJson.Schema,
                    Example = CreateMeasurementPostRequestExample()
                };
            }

            post.Responses["201"] = CreateJsonResponse("Вимірювання успішно створено.", GetJsonSchema(post.Responses, "201"), CreateMeasurementPostSuccessExample());
            post.Responses["400"] = CreateJsonResponse("Некоректні вхідні дані вимірювання.", GetJsonSchema(post.Responses, "400"), CreateMeasurementValidationErrorExample());
            post.Responses["404"] = CreateJsonResponse("Обладнання не знайдено.", GetJsonSchema(post.Responses, "404"), CreateMeasurementNotFoundErrorExample());
        }

        private static void ConfigureMeasurementGet(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Get, out var get))
            {
                return;
            }

            get.Summary = "Історія вимірювань обладнання";
            get.Description = "Повертає історію за equipmentId. Фільтри type, from, to необов’язкові й поєднуються через AND; межі часу включні.";

            var typeParameter = get.Parameters.FirstOrDefault(p => string.Equals(p.Name, "type", StringComparison.Ordinal));
            if (typeParameter is not null)
            {
                typeParameter.Description = "Необов’язковий тип: Temperature, Voltage, Load (case-insensitive).";
            }

            var fromParameter = get.Parameters.FirstOrDefault(p => string.Equals(p.Name, "from", StringComparison.Ordinal));
            if (fromParameter is not null)
            {
                fromParameter.Description = "Необов’язкова нижня межа часу ISO 8601 з Z/offset, включно.";
            }

            var toParameter = get.Parameters.FirstOrDefault(p => string.Equals(p.Name, "to", StringComparison.Ordinal));
            if (toParameter is not null)
            {
                toParameter.Description = "Необов’язкова верхня межа часу ISO 8601 з Z/offset, включно.";
            }

            get.Responses["200"] = CreateJsonResponse("Успішно. Повертає масив MeasurementDto (може бути порожнім).", GetJsonSchema(get.Responses, "200"), CreateMeasurementGetListExample());
            get.Responses["400"] = CreateJsonResponse("Некоректні параметри фільтрації.", GetJsonSchema(get.Responses, "400"), CreateMeasurementRangeErrorExample());
            get.Responses["404"] = CreateJsonResponse("Обладнання не знайдено.", GetJsonSchema(get.Responses, "404"), CreateMeasurementNotFoundErrorExample());
        }

        private static IOpenApiResponse CreateJsonResponse(string description, IOpenApiSchema? schema, JsonNode example)
        {
            return new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Example = example
                    }
                }
            };
        }

        private static IOpenApiSchema? GetJsonSchema(OpenApiResponses responses, string statusCode)
        {
            if (!responses.TryGetValue(statusCode, out var response) || response.Content is null)
            {
                return null;
            }

            return response.Content.TryGetValue("application/json", out var mediaType)
                ? mediaType.Schema
                : null;
        }

        private static void ConfigureTextProperty(IOpenApiSchema schema, string propertyName, string description)
        {
            if (!schema.Properties.TryGetValue(propertyName, out var property))
            {
                return;
            }

            property.Description = description;
        }

        private static JsonNode CreatePostRequestExample() => JsonNode.Parse("""
            {
              "name": "Compressor A1",
              "type": "Compressor",
              "serialNumber": "SN-2026-0001",
              "location": "Workshop 1",
              "status": "Working"
            }
            """)!;

        private static JsonNode CreatePostSuccessExample() => JsonNode.Parse("""
            {
              "id": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
              "name": "Compressor A1",
              "type": "Compressor",
              "serialNumber": "SN-2026-0001",
              "location": "Workshop 1",
              "status": "Working"
            }
            """)!;

        private static JsonNode CreateGetListExample() => JsonNode.Parse("""
            [
              {
                "id": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                "name": "Compressor A1",
                "type": "Compressor",
                "serialNumber": "SN-2026-0001",
                "location": "Workshop 1",
                "status": "Working"
              }
            ]
            """)!;

        private static JsonNode CreateValidationErrorExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Помилка валідації запиту.",
              "status": 400,
              "errors": {
                "Name": ["Вкажіть назву обладнання."],
                "Status": ["Вкажіть стан обладнання."]
              }
            }
            """)!;

        private static JsonNode CreateConflictExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
              "title": "Конфлікт унікальності серійного номера.",
              "status": 409,
              "errors": {
                "SerialNumber": ["Обладнання з таким серійним номером уже існує."]
              }
            }
            """)!;

        private static JsonNode CreateStatusErrorExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Помилка валідації запиту.",
              "status": 400,
              "errors": {
                "status": ["Вкажіть один із допустимих станів: Working, Faulty, UnderMaintenance."]
              }
            }
            """)!;

        private static JsonNode CreateMeasurementPostRequestExample() => JsonNode.Parse("""
            {
              "type": "Temperature",
              "value": -5.5,
              "unit": "°C",
              "measuredAt": "2026-10-24T12:30:00+02:00"
            }
            """)!;

        private static JsonNode CreateMeasurementPostSuccessExample() => JsonNode.Parse("""
            {
              "id": "9d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
              "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
              "type": "Temperature",
              "value": -5.5,
              "unit": "°C",
              "measuredAtUtc": "2026-10-24T10:30:00+00:00"
            }
            """)!;

        private static JsonNode CreateMeasurementGetListExample() => JsonNode.Parse("""
            [
              {
                "id": "1d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                "equipmentId": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                "type": "Voltage",
                "value": 220.0,
                "unit": "V",
                "measuredAtUtc": "2026-10-24T10:30:00+00:00"
              }
            ]
            """)!;

        private static JsonNode CreateMeasurementValidationErrorExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Помилка валідації запиту.",
              "status": 400,
              "errors": {
                "Type": ["Вкажіть тип показника."],
                "MeasuredAt": ["Вкажіть дату й час вимірювання."]
              }
            }
            """)!;

        private static JsonNode CreateMeasurementNotFoundErrorExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
              "title": "Обладнання не знайдено.",
              "status": 404,
              "errors": {
                "equipmentId": ["Обладнання з таким ідентифікатором не знайдено."]
              }
            }
            """)!;

        private static JsonNode CreateMeasurementRangeErrorExample() => JsonNode.Parse("""
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Помилка валідації запиту.",
              "status": 400,
              "errors": {
                "from": ["Параметр from не може бути пізнішим за to."],
                "to": ["Параметр to не може бути ранішим за from."]
              }
            }
            """)!;
    }
}
