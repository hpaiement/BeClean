using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    public class ChinookPgSqlDbFixture() : ChinookDbFixture(
        Environment.GetEnvironmentVariable(_connectionStringEnvVar) ?? _connectionString,
        (options, connectionString) => options.UseNpgsql(connectionString))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=postgres;Password=DevP4ssw0rdOnly!;TrustServerCertificate=True";

        /// <summary>
        /// Overrides the default connection string (e.g. when port 5432 is already used by another server). Must
        /// contain the "&lt;InsertDbNameHere&gt;" placeholder
        /// </summary>
        private const string _connectionStringEnvVar = "BECLEAN_PGSQL_CONNECTION_STRING";
    }
}
