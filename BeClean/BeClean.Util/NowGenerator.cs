namespace BeClean.Util
{
    /// <summary>
    /// Returns the actual datetime
    /// </summary>
    public interface INowGenerator
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
    }

    public class NowGenerator : INowGenerator
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;
    }
}
