dotnet ef dbcontext scaffold "Server=localhost;Database=Chinook;User=sa;Password=P4ssword!;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --project BeClean.DataLayer.IntegrationTest.Db --output-dir Chinook/Models --context ChinookContext --context-dir Chinook/DataContext --force

dotnet ef migrations add InitData --project BeClean.DataLayer.IntegrationTest.Db --output-dir Chinook/Migrations --startup-project BeClean.DataLayer.IntegrationTest.Db --context BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext.ChinookContext

dotnet ef migrations remove --project BeClean.DataLayer.IntegrationTest.Db --startup-project BeClean.DataLayer.IntegrationTest.Db --context BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext.ChinookContext