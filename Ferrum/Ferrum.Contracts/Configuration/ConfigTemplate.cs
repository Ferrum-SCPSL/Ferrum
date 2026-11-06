using System;
using System.Globalization;

namespace Ferrum.Contracts;

public static class ConfigTemplate
{
    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    public static string Create(FerrumConfig config) => string.Join(Environment.NewLine, new[]
    {
        "# Whether Ferrum is enabled on this server.", "is_enabled: " + config.IsEnabled.ToString().ToLowerInvariant(),
        "# Whether debug messages should be shown.", "debug: " + config.Debug.ToString().ToLowerInvariant(),
        "# Ferrum data directory. Empty uses ~/.config/Ferrum; FERRUM_HOME overrides this.", "ferrum_directory_path: " + Quote(config.FerrumDirectoryPath),
        "# Framework log language: auto, en, ru, uk, zh-CN.", "log_language: " + Quote(config.LogLanguage),
        "# Default language for plugins supporting Ferrum localization.", "plugin_language: " + Quote(config.PluginLanguage),
        "# Update channel: stable or stable_and_prerelease.", "update_channel: " + Quote(config.UpdateChannel),
        "# Whether Ferrum should download and prepare updates automatically.", "enable_auto_updates: " + config.EnableAutoUpdates.ToString().ToLowerInvariant(),
        "# Interval between update checks, in minutes.", "update_check_interval_minutes: " + config.UpdateCheckIntervalMinutes.ToString(CultureInfo.InvariantCulture),
        "# Whether a prepared update should schedule a server restart after the round.", "restart_after_update: " + config.RestartAfterUpdate.ToString().ToLowerInvariant(), string.Empty,
    });

    public static void Validate(FerrumConfig config)
    {
        if (config.UpdateChannel != "stable" && config.UpdateChannel != "stable_and_prerelease") throw new ArgumentException("Invalid update_channel.");
        if (config.UpdateCheckIntervalMinutes < 1 || config.UpdateCheckIntervalMinutes > 10080) throw new ArgumentException("Invalid update_check_interval_minutes.");
        foreach (string? language in new[] { config.LogLanguage, config.PluginLanguage })
            if (language != "auto" && language != "en" && language != "ru" && language != "uk" && language != "zh-CN")
                throw new ArgumentException("Invalid language.");
    }
}
