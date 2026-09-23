namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;

/// <summary>
/// Test only entity (not part of Chinook) whose table is in an explicit schema and whose column names differ from the
/// property names, one of them being a reserved SQL word. See <see cref="DataContext.ChinookContext"/> mapping.
/// </summary>
public class MappedItem
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Label { get; set; }

    /// <summary>
    /// Mapped to the "Order" column (reserved word)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Import batch number, used to scope synchronizations
    /// </summary>
    public int Batch { get; set; }
}
