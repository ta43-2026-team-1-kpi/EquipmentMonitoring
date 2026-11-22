using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    public static class PostgresConnectionString
    {
        public const string Name = "EquipmentDatabase";
        public const string PasswordFileKey = "Database:PasswordFile";

        /// <summary>
        /// Reads ConnectionStrings:EquipmentDatabase. When Database:PasswordFile is set (Docker secret),
        /// the password is taken from that file, so it never appears in environment variables.
        /// </summary>
        public static string Resolve(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(Name);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{Name}' is not configured. " +
                    "For Docker Compose run, use .env and docker-compose.yaml. " +
                    $"For local non-Docker run, set ConnectionStrings__{Name} to a PostgreSQL connection string.");
            }

            var passwordFile = configuration[PasswordFileKey];
            if (string.IsNullOrWhiteSpace(passwordFile))
            {
                return connectionString;
            }

            if (!File.Exists(passwordFile))
            {
                throw new InvalidOperationException($"Database password file '{passwordFile}' does not exist.");
            }

            var password = File.ReadAllText(passwordFile).TrimEnd('\r', '\n');
            if (password.Length == 0)
            {
                throw new InvalidOperationException($"Database password file '{passwordFile}' is empty.");
            }

            return new NpgsqlConnectionStringBuilder(connectionString) { Password = password }.ConnectionString;
        }
    }
}
