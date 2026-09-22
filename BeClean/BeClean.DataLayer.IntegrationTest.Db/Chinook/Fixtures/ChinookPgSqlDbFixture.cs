using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    public class ChinookPgSqlDbFixture() : ChinookDbFixture(
        _connectionString,
        (options, connectionString) => options.UseNpgsql(connectionString))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=postgres;Password=DevP4ssw0rdOnly!;TrustServerCertificate=True";
    }
}
