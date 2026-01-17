using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BeClean.Api
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MapAppVersion(this WebApplication app, string url, string versionFilePath)
        {
            app.MapGet(url, async () =>
            {
                var versionJson = await File.ReadAllTextAsync(versionFilePath);
                var version = JsonSerializer.Deserialize<VersionDto>(versionJson);


                return Results.Json(version, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

            })
            //.WithName("AppVersion")
            //.WithTags("Diagnostics")
            .Produces(200, typeof(VersionDto), "application/json");

            return app;
        }
    }
}
