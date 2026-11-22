using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public sealed class PostgresConnectionStringTests : IDisposable
    {
        private const string ConnectionString = "Host=db;Port=5432;Database=equipment_monitoring;Username=equipment_user";

        private readonly string _passwordFile = Path.GetTempFileName();

        [Fact]
        public void Resolve_WithoutPasswordFile_ShouldReturnConfiguredConnectionString()
        {
            var resolved = PostgresConnectionString.Resolve(Configuration(ConnectionString + ";Password=from-env", passwordFile: null));

            Assert.Equal("from-env", new NpgsqlConnectionStringBuilder(resolved).Password);
        }

        [Fact]
        public void Resolve_WithPasswordFile_ShouldTakePasswordFromFile()
        {
            File.WriteAllText(_passwordFile, "s3cr;et=\"value\"\n");

            var resolved = PostgresConnectionString.Resolve(Configuration(ConnectionString + ";Password=ignored", _passwordFile));

            var builder = new NpgsqlConnectionStringBuilder(resolved);
            Assert.Equal("s3cr;et=\"value\"", builder.Password);
            Assert.Equal("db", builder.Host);
            Assert.Equal("equipment_user", builder.Username);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\n")]
        public void Resolve_WithEmptyPasswordFile_ShouldThrow(string content)
        {
            File.WriteAllText(_passwordFile, content);

            var exception = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Resolve(Configuration(ConnectionString, _passwordFile)));
            Assert.Contains("is empty", exception.Message);
        }

        [Fact]
        public void Resolve_WithMissingPasswordFile_ShouldThrow()
        {
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            var exception = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Resolve(Configuration(ConnectionString, missing)));
            Assert.Contains(missing, exception.Message);
        }

        [Fact]
        public void Resolve_WithoutConnectionString_ShouldThrow()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Resolve(Configuration(null, passwordFile: null)));
            Assert.Contains("EquipmentDatabase", exception.Message);
        }

        public void Dispose()
        {
            File.Delete(_passwordFile);
        }

        private static IConfiguration Configuration(string? connectionString, string? passwordFile)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EquipmentDatabase"] = connectionString,
                    [PostgresConnectionString.PasswordFileKey] = passwordFile
                })
                .Build();
        }
    }
}
