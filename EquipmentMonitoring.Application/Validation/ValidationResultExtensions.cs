namespace EquipmentMonitoring.Application.Validation
{
    internal static class ValidationResultExtensions
    {
        public static void AddError(this EquipmentValidationResult result, string fieldName, string message)
        {
            if (!result.Errors.TryGetValue(fieldName, out var messages))
            {
                messages = [];
                result.Errors[fieldName] = messages;
            }

            messages.Add(message);
        }
    }
}
