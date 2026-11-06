namespace Ferrum.Contracts;

public interface ILocalizer
{
    string GetMessage(string key, params object[] arguments);
}

public interface ILocalizationService : ILocalizer
{
    string LogLanguage { get; }
    string PluginLanguage { get; }
    ILocalizer RegisterPlugin(string pluginId, string catalogDirectory);
}

/// <summary>Use after Ferrum has initialized. Plugin localizers inherit PluginLanguage.</summary>
public static class FerrumServices
{
    public static ILocalizationService? Localization { get; set; }
}
