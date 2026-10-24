using EquipmentMonitoring.Application.DTOs.Equipment;
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

                ConfigureTextProperty(
                    createSchema,
                    nameof(CreateEquipmentDto.Name),
                    "Назва обладнання. Обов’язкове поле, максимум 200 символів.");
                ConfigureTextProperty(
                    createSchema,
                    nameof(CreateEquipmentDto.Type),
                    "Тип обладнання. Обов’язкове поле, максимум 100 символів.");
                ConfigureTextProperty(
                    createSchema,
                    nameof(CreateEquipmentDto.SerialNumber),
                    "Серійний номер обладнання. Обов’язкове поле, максимум 100 символів.");
                ConfigureTextProperty(
                    createSchema,
                    nameof(CreateEquipmentDto.Location),
                    "Місце встановлення обладнання. Обов’язкове поле, максимум 200 символів.");

                if (createSchema.Properties.TryGetValue(nameof(CreateEquipmentDto.Status), out var statusProperty))
                {
                    statusProperty.Description = "Стан обладнання. Обов’язкове поле. Допустимі значення: Working, Faulty, UnderMaintenance.";
                }
            }

            if (document.Components.Schemas.TryGetValue(nameof(EquipmentDto), out var equipmentSchema))
            {
                equipmentSchema.Description = "Дані зареєстрованого обладнання.";

                if (equipmentSchema.Properties.TryGetValue(nameof(EquipmentDto.Id), out var idProperty))
                {
                    idProperty.Description = "Унікальний ідентифікатор обладнання (Guid), генерується системою.";
                }

                ConfigureTextProperty(
                    equipmentSchema,
                    nameof(EquipmentDto.Name),
                    "Назва обладнання.");
                ConfigureTextProperty(
                    equipmentSchema,
                    nameof(EquipmentDto.Type),
                    "Тип обладнання.");
                ConfigureTextProperty(
                    equipmentSchema,
                    nameof(EquipmentDto.SerialNumber),
                    "Серійний номер обладнання.");
                ConfigureTextProperty(
                    equipmentSchema,
                    nameof(EquipmentDto.Location),
                    "Місце встановлення обладнання.");

                if (equipmentSchema.Properties.TryGetValue(nameof(EquipmentDto.Status), out var statusProperty))
                {
                    statusProperty.Description = "Поточний стан обладнання. Можливі значення: Working, Faulty, UnderMaintenance.";
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

            ConfigurePost(path.Value);
            ConfigureGet(path.Value);
        }

        private static void ConfigurePost(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Post, out var post))
            {
                return;
            }

            post.Responses.Remove("200");

            post.Summary = "Реєстрація обладнання";
            post.Description = "Створює новий запис обладнання. Усі поля обов’язкові. Статус передається рядком: Working, Faulty або UnderMaintenance.";

            if (post.RequestBody?.Content.TryGetValue("application/json", out var requestJson) == true)
            {
                post.RequestBody.Content["application/json"] = new OpenApiMediaType
                {
                    Schema = requestJson.Schema,
                    Example = CreatePostRequestExample()
                };
            }

            post.Responses["201"] = CreateJsonResponse(
                "Обладнання успішно зареєстровано.",
                GetJsonSchema(post.Responses, "201"),
                CreatePostSuccessExample());

            post.Responses["400"] = CreateJsonResponse(
                "Некоректні вхідні дані або дубль серійного номера, виявлений попередньою перевіркою.",
                GetJsonSchema(post.Responses, "400"),
                CreateValidationErrorExample());

            post.Responses["409"] = CreateJsonResponse(
                "Конфлікт унікальності серійного номера під час збереження.",
                GetJsonSchema(post.Responses, "409"),
                CreateConflictExample());
        }

        private static void ConfigureGet(IOpenApiPathItem pathItem)
        {
            if (!pathItem.Operations.TryGetValue(HttpMethod.Get, out var get))
            {
                return;
            }

            get.Summary = "Перелік обладнання та фільтрація за станом";
            get.Description = "Повертає перелік обладнання. Якщо параметр status відсутній — повертаються всі записи. Якщо status передано, допускаються лише Working, Faulty, UnderMaintenance без урахування регістру. Порожні, числові та невідомі значення повертають 400.";

            var statusParameter = get.Parameters.FirstOrDefault(p => string.Equals(p.Name, "status", StringComparison.Ordinal));
            if (statusParameter is not null)
            {
                statusParameter.Description = "Необов’язковий фільтр за станом (Working, Faulty, UnderMaintenance). Без параметра — всі записи.";
            }

            get.Responses["200"] = CreateJsonResponse(
                "Успішно. Повертає масив EquipmentDto (може бути порожнім).",
                GetJsonSchema(get.Responses, "200"),
                CreateGetListExample());

            get.Responses["400"] = CreateJsonResponse(
                "Некоректне значення параметра status.",
                GetJsonSchema(get.Responses, "400"),
                CreateStatusErrorExample());
        }

        private static IOpenApiResponse CreateJsonResponse(
            string description,
            IOpenApiSchema? schema,
            JsonNode example)
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
            if (!responses.TryGetValue(statusCode, out var response))
            {
                return null;
            }

            if (response.Content is null)
            {
                return null;
            }

            if (!response.Content.TryGetValue("application/json", out var mediaType))
            {
                return null;
            }

            return mediaType.Schema;
        }

        private static void ConfigureTextProperty(
            IOpenApiSchema schema,
            string propertyName,
            string description)
        {
            if (!schema.Properties.TryGetValue(propertyName, out var property))
            {
                return;
            }

            property.Description = description;
        }

        private static JsonNode CreatePostRequestExample()
        {
            return JsonNode.Parse("""
                {
                  "name": "Compressor A1",
                  "type": "Compressor",
                  "serialNumber": "SN-2026-0001",
                  "location": "Workshop 1",
                  "status": "Working"
                }
                """)!;
        }

        private static JsonNode CreatePostSuccessExample()
        {
            return JsonNode.Parse("""
                {
                  "id": "8d65a7de-5d4e-4f08-bdc8-ff4cfd8f40b8",
                  "name": "Compressor A1",
                  "type": "Compressor",
                  "serialNumber": "SN-2026-0001",
                  "location": "Workshop 1",
                  "status": "Working"
                }
                """)!;
        }

        private static JsonNode CreateGetListExample()
        {
            return JsonNode.Parse("""
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
        }

        private static JsonNode CreateValidationErrorExample()
        {
            return JsonNode.Parse("""
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
        }

        private static JsonNode CreateConflictExample()
        {
            return JsonNode.Parse("""
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                  "title": "Конфлікт унікальності серійного номера.",
                  "status": 409,
                  "errors": {
                    "SerialNumber": ["Обладнання з таким серійним номером уже існує."]
                  }
                }
                """)!;
        }

        private static JsonNode CreateStatusErrorExample()
        {
            return JsonNode.Parse("""
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "Помилка валідації запиту.",
                  "status": 400,
                  "errors": {
                    "status": ["Вкажіть один із допустимих станів: Working, Faulty, UnderMaintenance."]
                  }
                }
                """)!;
        }
    }
}
