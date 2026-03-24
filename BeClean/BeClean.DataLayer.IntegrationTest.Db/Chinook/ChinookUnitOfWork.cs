using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.Repository;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook;

public interface IChinookUnitOfWork : IUnitOfWork
{
}

public class ChinookUnitOfWork(
   ChinookContext dbContext
) : UnitOfWork.UnitOfWork(dbContext), IChinookUnitOfWork
{
}