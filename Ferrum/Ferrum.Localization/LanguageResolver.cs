using System;
using System.Globalization;

#if FERRUM_BOOTSTRAP
namespace Ferrum.Loader.Localization;
#else
namespace Ferrum.Localization;
#endif

public static class LanguageResolver
{
    public static string Resolve(string? requested)
    {
        string language = requested ?? "auto";
        if (language.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            language = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrWhiteSpace(language) || language == "C" || language == "POSIX")
                language = Environment.GetEnvironmentVariable("LC_ALL")
                    ?? Environment.GetEnvironmentVariable("LC_MESSAGES")
                    ?? Environment.GetEnvironmentVariable("LANG") ?? "en";
        }
        language = language.Replace('_', '-').Split('.')[0].Split('@')[0].ToLowerInvariant();
        if (language == "ru" || language.StartsWith("ru-", StringComparison.Ordinal)) return "ru";
        if (language == "uk" || language.StartsWith("uk-", StringComparison.Ordinal)) return "uk";
        if (language == "zh" || language == "zh-cn" || language == "zh-sg" || language.StartsWith("zh-hans", StringComparison.Ordinal)) return "zh-CN";
        return "en";
    }
}
