namespace EquipmentMonitoring.Application.Exceptions
{
    /// <summary>
    /// Thrown when the row was changed by another request after it was loaded.
    /// Tracked entities are refreshed from the database before this is thrown.
    /// </summary>
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(Exception innerException)
            : base("Запис змінено іншим запитом.", innerException)
        {
        }
    }
}
