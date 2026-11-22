namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    public class ComposeFileTests
    {
        [Theory]
        [InlineData(ComposeFile.Production)]
        [InlineData(ComposeFile.Monitoring)]
        public void Overlay_ShouldUseSamePostgresImageAsDatabase(string fileName)
        {
            // pg_dump refuses to dump a newer server, and psql tooling should match the server it manages.
            var images = ComposeFile.GetPostgresImages(fileName);

            Assert.NotEmpty(images);
            Assert.All(images, image => Assert.Equal(ComposeFile.PostgresImage, image));
        }
    }
}
