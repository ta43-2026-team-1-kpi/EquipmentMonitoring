using System.Globalization;
using System.Text.RegularExpressions;

namespace EquipmentMonitoring.Application.Validation
{
    public static class StrictDateTimeOffsetParser
    {
        private static readonly Regex DateTimeOffsetPattern = new(
            "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{1,7})?(?:Z|[+-]\\d{2}:\\d{2})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] AllowedFormats =
        [
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"
        ];

        public static bool TryParse(string input, out DateTimeOffset parsed)
        {
            parsed = default;

            if (!DateTimeOffsetPattern.IsMatch(input))
            {
                return false;
            }

            return DateTimeOffset.TryParseExact(
                input,
                AllowedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed);
        }
    }
}
