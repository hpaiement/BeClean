namespace BeClean.TestLib
{
    /// <summary>
    /// A reusable set of test data that can be seeded into a database context.
    /// Implementations should expose any value the tests need to reference (keys, names, ...)
    /// so tests don't have to query them back.
    /// </summary>
    /// <typeparam name="TContext">Database context type the data is seeded into</typeparam>
    public interface IDataFixture<TContext>
    {
        void Seed(TContext dbContext);
    }
}
