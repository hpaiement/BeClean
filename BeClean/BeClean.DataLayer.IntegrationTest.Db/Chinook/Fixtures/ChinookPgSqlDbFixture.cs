using BeClean.DataLayer.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    /// <param name="useBeCleanBulk">Enables bulk operations on the db context options</param>
    public class ChinookPgSqlDbFixture(bool useBeCleanBulk = true) : ChinookDbFixture(
        Environment.GetEnvironmentVariable(_connectionStringEnvVar) ?? _connectionString,
        (options, connectionString) => options.UseNpgsql(connectionString, o => { if (useBeCleanBulk) o.UseBeCleanBulk(); }))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=postgres;Password=DevP4ssw0rdOnly!;TrustServerCertificate=True";

        /// <summary>
        /// Overrides the default connection string (e.g. when port 5432 is already used by another server). Must
        /// contain the "&lt;InsertDbNameHere&gt;" placeholder
        /// </summary>
        private const string _connectionStringEnvVar = "BECLEAN_PGSQL_CONNECTION_STRING";
    }
}
