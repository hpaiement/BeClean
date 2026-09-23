using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    public class ChinookSqlServerDbFixture() : ChinookDbFixture(
        Environment.GetEnvironmentVariable(_connectionStringEnvVar) ?? _connectionString,
        (options, connectionString) => options.UseSqlServer(connectionString))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=sa;Password=DevP4ssw0rdOnly!;MultipleActiveResultSets=True;TrustServerCertificate=True";

        /// <summary>
        /// Overrides the default connection string (e.g. when port 1433 is already used by another server). Must
        /// contain the "&lt;InsertDbNameHere&gt;" placeholder
        /// </summary>
        private const string _connectionStringEnvVar = "BECLEAN_SQLSERVER_CONNECTION_STRING";
    }
}
