using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    internal static class SqliteErrors
    {
        private const int ConstraintUnique = 2067;
        private const int ConstraintForeignKey = 787;
        private const string PostgresUniqueViolationSqlState = "23505";
        private const string PostgresForeignKeyViolationSqlState = "23503";

        public static bool IsUniqueViolation(DbUpdateException exception)
        {
            return HasExtendedErrorCode(exception, ConstraintUnique)
                || HasPostgresSqlState(exception, PostgresUniqueViolationSqlState);
        }

        public static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return HasExtendedErrorCode(exception, ConstraintForeignKey)
                || HasPostgresSqlState(exception, PostgresForeignKeyViolationSqlState);
        }

        public static bool IsPostgresConstraint(DbUpdateException exception, string constraintName)
        {
            return exception.InnerException is PostgresException postgresException
                && string.Equals(postgresException.ConstraintName, constraintName, StringComparison.Ordinal);
        }

        private static bool HasExtendedErrorCode(DbUpdateException exception, int errorCode)
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteExtendedErrorCode == errorCode;
        }

        private static bool HasPostgresSqlState(DbUpdateException exception, string sqlState)
        {
            return exception.InnerException is PostgresException postgresException
                && string.Equals(postgresException.SqlState, sqlState, StringComparison.Ordinal);
        }
    }
}
