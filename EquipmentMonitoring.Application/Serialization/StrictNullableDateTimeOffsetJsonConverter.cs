using System.Text.Json;
using System.Text.Json.Serialization;
using EquipmentMonitoring.Application.Validation;

namespace EquipmentMonitoring.Application.Serialization
{
    public sealed class StrictNullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Поле MeasuredAt має бути рядком у форматі ISO 8601 із часовим зсувом.");
            }

            var text = reader.GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new JsonException("Поле MeasuredAt не може бути порожнім.");
            }

            if (!StrictDateTimeOffsetParser.TryParse(text, out var parsed))
            {
                throw new JsonException("Поле MeasuredAt має бути у валідному форматі ISO 8601.");
            }

            return parsed;
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTimeOffset? value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToString("O"));
        }
    }
}
