using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Ferrum.Contracts;

#if FERRUM_BOOTSTRAP
namespace Ferrum.Loader.Localization;
#else
namespace Ferrum.Localization;
#endif

public sealed class CatalogLocalizer : ILocalizer
{
    private readonly List<Dictionary<string, string>> catalogs = new();
    private readonly CultureInfo culture;

    public CatalogLocalizer(string language, string? directory, bool framework, Action<string>? malformed = null)
    {
        culture = CultureInfo.GetCultureInfo(language);
        AddExternal(directory, language, malformed);
        if (framework) AddEmbedded(language);
        if (language != "en") { AddExternal(directory, "en", malformed); if (framework) AddEmbedded("en"); }
    }

    private void AddExternal(string? directory, string language, Action<string>? malformed)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;
        string path = Path.Combine(directory!, language + ".csv");
        if (!File.Exists(path)) return;
        try { using var reader = new StreamReader(path, new UTF8Encoding(false, true)); catalogs.Add(CsvCatalog.Read(reader)); }
        catch (Exception error) when (error is IOException || error is ArgumentException || error is DecoderFallbackException)
        { malformed?.Invoke(path); }
    }

    private void AddEmbedded(string language)
    {
        using Stream? stream = typeof(CatalogLocalizer).Assembly.GetManifestResourceStream("Ferrum.Localization.Catalogs." + language + ".csv");
        if (stream == null) return;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        catalogs.Add(CsvCatalog.Read(reader));
    }

    public string GetMessage(string key, params object[] arguments)
    {
        foreach (var catalog in catalogs)
        {
            if (!catalog.TryGetValue(key, out string? text)) continue;
            try { return string.Format(culture, text, arguments); }
            catch (FormatException) { /* A broken override must not prevent fallback. */ }
        }
        return key;
    }
}
