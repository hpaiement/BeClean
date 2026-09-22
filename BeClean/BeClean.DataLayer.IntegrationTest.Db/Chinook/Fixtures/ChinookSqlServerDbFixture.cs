using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    public class ChinookSqlServerDbFixture() : ChinookDbFixture(
        _connectionString,
        (options, connectionString) => options.UseSqlServer(connectionString))
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=sa;Password=DevP4ssw0rdOnly!;MultipleActiveResultSets=True;TrustServerCertificate=True";
    }
}
