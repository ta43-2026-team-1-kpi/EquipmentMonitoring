using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Application.Serialization;
using System.Text.Json.Serialization;

namespace EquipmentMonitoring.Application.DTOs.Measurement
{
    public class CreateMeasurementDto
    {
        public MeasurementType? Type { get; set; }

        public double? Value { get; set; }

        public string? Unit { get; set; }

        [JsonConverter(typeof(StrictNullableDateTimeOffsetJsonConverter))]
        public DateTimeOffset? MeasuredAt { get; set; }
    }
}
