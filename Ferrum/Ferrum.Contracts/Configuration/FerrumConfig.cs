namespace Ferrum.Contracts;

/// <summary>Per-port configuration shared by the bootstrap and versioned runtime.</summary>
public sealed class FerrumConfig
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; }
    public string FerrumDirectoryPath { get; set; } = string.Empty;
    public string LogLanguage { get; set; } = "auto";
    public string PluginLanguage { get; set; } = "auto";
    public string UpdateChannel { get; set; } = "stable";
    public bool EnableAutoUpdates { get; set; } = true;
    public int UpdateCheckIntervalMinutes { get; set; } = 360;
    public bool RestartAfterUpdate { get; set; } = true;
}
