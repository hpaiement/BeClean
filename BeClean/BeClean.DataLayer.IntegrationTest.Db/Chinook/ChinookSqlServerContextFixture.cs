using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.TestLib;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook
{
    public class ChinookSqlServerContextFixture : IDisposable
    {
        private const string _connectionString = "Server=localhost;Database=<InsertDbNameHere>;User Id=sa;Password=P4ssword!;MultipleActiveResultSets=True;TrustServerCertificate=True";

        public readonly ChinookContext dbContext;

        public ChinookSqlServerContextFixture()
        {
            var options = new DbContextOptionsBuilder<ChinookContext>()
                //.UseNpgsql((_connectionString ?? "").Replace("<InsertDbNameHere>", $"{DateTime.Now.ToString("yyMMddHHmm")}_{Guid.NewGuid().ToString().Replace("-", "")}"))
                .UseSqlServer((_connectionString ?? "").Replace("<InsertDbNameHere>", $"{DateTime.Now.ToString("yyMMddHHmm")}_{Guid.NewGuid().ToString().Replace("-", "")}"))
                //.EnableSensitiveDataLogging()
                .Options;

            dbContext = new ChinookContext(options);
            dbContext.Database.EnsureCreated();

            var sql = File.ReadAllText(Path.Join(TestDirectories.ProjectDirectory, "Chinook", "Data", "InsertData.SqlServer.sql"));
            dbContext.Database.ExecuteSqlRaw(sql);
        }

        public void Dispose()
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Dispose();
        }
    }
}
