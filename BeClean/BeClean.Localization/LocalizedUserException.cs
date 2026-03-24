namespace BeClean.Localization
{
    public class LocalizedUserException : Exception
    {
        public object[] Args {  get; init; }

        public LocalizedUserException(string messageKey, params object[] args) : base(messageKey)
        {
            Args = args;
        }
    }
}
