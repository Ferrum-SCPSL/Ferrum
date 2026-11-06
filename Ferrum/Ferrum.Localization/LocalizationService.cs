using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ferrum.Contracts;

#if FERRUM_BOOTSTRAP
namespace Ferrum.Loader.Localization;
#else
namespace Ferrum.Localization;
#endif

public sealed class LocalizationService : ILocalizationService
{
    private readonly CatalogLocalizer framework;
    public string LogLanguage { get; }
    public string PluginLanguage { get; }

    public LocalizationService(string logLanguage, string pluginLanguage, string? directory, Action<string>? malformed = null)
    {
        LogLanguage = LanguageResolver.Resolve(logLanguage);
        PluginLanguage = LanguageResolver.Resolve(pluginLanguage);
        framework = new CatalogLocalizer(LogLanguage, directory, true, malformed);
    }

    public string GetMessage(string key, params object[] arguments) => framework.GetMessage(key, arguments);

    public ILocalizer RegisterPlugin(string pluginId, string catalogDirectory)
    {
        if (!Regex.IsMatch(pluginId ?? "", @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$")) throw new ArgumentException(nameof(pluginId));
        return new CatalogLocalizer(PluginLanguage, Path.GetFullPath(catalogDirectory), false);
    }

    public static void SeedCatalogs(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (string language in new[] { "en", "ru", "uk", "zh-CN" })
        {
            string destination = Path.Combine(directory, language + ".csv");
            if (File.Exists(destination)) continue;
            using Stream source = typeof(LocalizationService).Assembly.GetManifestResourceStream("Ferrum.Localization.Catalogs." + language + ".csv")!;
            using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);
            source.CopyTo(target);
        }
    }
}
