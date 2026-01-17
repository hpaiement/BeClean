using Microsoft.Extensions.Localization;

namespace BeClean.Localization
{
    public sealed class LocaleStringService
    {
        private readonly IStringLocalizer<LocaleStringService> _localizer = null!;

        public LocaleStringService(IStringLocalizer<LocaleStringService> localizer) => _localizer = localizer;

        public string GetLocaleString(string key) => _localizer.GetString(key);
    }
}