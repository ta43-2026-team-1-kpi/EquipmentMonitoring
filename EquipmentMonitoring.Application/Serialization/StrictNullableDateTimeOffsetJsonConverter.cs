using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace EquipmentMonitoring.Application.Serialization
{
    public sealed class StrictNullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
    {
        private static readonly Regex OffsetPattern = new(
            "(Z|[+-]\\d{2}:\\d{2})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

            if (!OffsetPattern.IsMatch(text))
            {
                throw new JsonException("Поле MeasuredAt має містити часовий зсув або суфікс Z.");
            }

            if (!DateTimeOffset.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
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

            writer.WriteStringValue(value.Value.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
