namespace EquipmentMonitoring.Domain.Common
{
    internal static class Guard
    {
        public static void Utc(DateTime value, string paramName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Timestamp must be in UTC.", paramName);
            }
        }

        public static void Defined<TEnum>(TEnum value, string paramName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Undefined {typeof(TEnum).Name} value.");
            }
        }

        public static void Finite(double? value, string paramName)
        {
            if (value is not null && !double.IsFinite(value.Value))
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be a finite number.");
            }
        }

        public static void NotEmpty(Guid value, string paramName)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Identifier must not be empty.", paramName);
            }
        }
    }
}
