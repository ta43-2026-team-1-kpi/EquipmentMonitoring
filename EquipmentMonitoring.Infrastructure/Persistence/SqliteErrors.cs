using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    internal static class SqliteErrors
    {
        private const int ConstraintUnique = 2067;
        private const int ConstraintForeignKey = 787;

        public static bool IsUniqueViolation(DbUpdateException exception)
        {
            return HasExtendedErrorCode(exception, ConstraintUnique);
        }

        public static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return HasExtendedErrorCode(exception, ConstraintForeignKey);
        }

        private static bool HasExtendedErrorCode(DbUpdateException exception, int errorCode)
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteExtendedErrorCode == errorCode;
        }
    }
}
