using BeClean.DataLayer.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    /// <param name="useBeCleanBulk">Enables bulk operations on the db context options</param>
    public class ChinookSqlServerDbFixture(bool useBeCleanBulk = true) : ChinookDbFixture(
        Environment.GetEnvironmentVariable(_connectionStringEnvVar) ?? _connectionString,
        (options, connectionString) => options.UseSqlServer(connectionString, o => { if (useBeCleanBulk) o.UseBeCleanBulk(); }))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=sa;Password=DevP4ssw0rdOnly!;MultipleActiveResultSets=True;TrustServerCertificate=True";

        /// <summary>
        /// Overrides the default connection string (e.g. when port 1433 is already used by another server). Must
        /// contain the "&lt;InsertDbNameHere&gt;" placeholder
        /// </summary>
        private const string _connectionStringEnvVar = "BECLEAN_SQLSERVER_CONNECTION_STRING";
    }
}
