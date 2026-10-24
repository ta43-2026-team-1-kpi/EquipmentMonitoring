namespace EquipmentMonitoring.Application.Validation
{
    public class EquipmentValidationResult
    {
        public Dictionary<string, List<string>> Errors { get; } = new();

        public bool IsValid => Errors.Count == 0;
    }
}
