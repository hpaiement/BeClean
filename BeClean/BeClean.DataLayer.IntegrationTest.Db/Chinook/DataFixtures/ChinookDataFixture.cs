using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.TestLib;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.DataFixtures
{
    /// <summary>
    /// The full sample Chinook dataset (artists, albums, tracks, invoices, ...)
    /// </summary>
    public class ChinookDataFixture : IDataFixture<ChinookContext>
    {
        public void Seed(ChinookContext dbContext)
        {
            var scriptName = dbContext.Database switch
            {
                var db when db.IsNpgsql() => "InsertData.PgSql.sql",
                var db when db.IsSqlServer() => "InsertData.SqlServer.sql",
                _ => throw new NotSupportedException($"No Chinook seed script for database provider {dbContext.Database.ProviderName}")
            };

            var sql = File.ReadAllText(Path.Join(TestDirectories.GetProjectDirectory(typeof(ChinookDataFixture)), "Chinook", "Data", scriptName));
            dbContext.Database.ExecuteSqlRaw(sql);
        }
    }
}
