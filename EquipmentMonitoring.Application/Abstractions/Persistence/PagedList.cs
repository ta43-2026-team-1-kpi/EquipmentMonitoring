namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount);
}
