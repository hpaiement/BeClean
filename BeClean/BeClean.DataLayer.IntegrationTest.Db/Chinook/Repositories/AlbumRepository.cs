using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using BeClean.DataLayer.Repositories.Bulk;
using BeClean.Repository;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Repositories
{
    public class AlbumRepository(
        ChinookContext dbContext,
        IChinookUnitOfWork unitOfWork
    ) : EFBulkRepository<Album, ChinookContext>(dbContext, unitOfWork), IEFBulkRepository<Album>
    {
    }
}
