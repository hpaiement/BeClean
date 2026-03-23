namespace BeClean.Localization
{
    public class LocalizedUnauthorizedException : UnauthorizedAccessException
    {
        public object[] Args { get; init; }

        public LocalizedUnauthorizedException(string messageKey, params object[] args) : base(messageKey)
        {
            Args = args;
        }
    }
}
