using System.Text.RegularExpressions;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    /// <summary>
    /// Reads the compose files copied to the test output, so tests run against the same PostgreSQL image as the stack.
    /// </summary>
    internal static partial class ComposeFile
    {
        public const string Base = "docker-compose.yaml";

        public static string PostgresImage { get; } = GetPostgresImages(Base).FirstOrDefault()
            ?? throw new InvalidOperationException($"PostgreSQL image is not declared in {Base}.");

        public static IReadOnlyList<string> GetPostgresImages(string fileName)
        {
            var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "compose", fileName));

            return PostgresImagePattern().Matches(content).Select(m => m.Groups["image"].Value).ToList();
        }

        [GeneratedRegex(@"^\s*image:\s*[""']?(?<image>postgres:[^\s""']+)[""']?\s*(#.*)?$", RegexOptions.Multiline)]
        private static partial Regex PostgresImagePattern();
    }
}
