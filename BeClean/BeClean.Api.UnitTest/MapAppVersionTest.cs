using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using BeClean.TestLib;
using System.Text.Json;

namespace BeClean.Api.UnitTest
{
    public class MapAppVersionTest
    {
        [Fact]
        public async Task GetVersionOfTheApp()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            var app = builder.Build();

            app.MapAppVersion("Version", Path.Join(TestDirectories.GetProjectDirectory(typeof(MapAppVersionTest)), "Data", "version.json"));

            await app.StartAsync();

            var client = app.GetTestClient();

            // Act
            var response = await client.GetAsync("Version");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var stringContent = await response.Content.ReadAsStringAsync();
            var version = JsonSerializer.Deserialize<VersionDto>(stringContent);
            Assert.NotNull(version);
            Assert.Equal("1.0.14", version.Version);
            Assert.Equal("admin", version.DeployedBy);
            Assert.Equal(DateTime.Parse("2026-01-09T12:21:29.3956963-05:00"), version.BuildDate);
        }
    }
}
