using System;
using System.IO;

namespace Ferrum.Loader.Features.Paths;

/// <summary>Ferrum's own data directories, independent of LabAPI's deployment paths.</summary>
public static class FerrumPaths
{
    public static string Root { get; } = ResolveRoot();

    public static string Configs => Path.Combine(Root, "Configs");

    public static string Logs => Path.Combine(Root, "Logs");

    public static string Plugins => Path.Combine(Root, "Plugins");

    public static string Dependencies => Path.Combine(Plugins, "Dependencies");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(Configs);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Plugins);
        Directory.CreateDirectory(Dependencies);
    }

    private static string ResolveRoot()
    {
        // Explicit override keeps dedicated-server smoke runs isolated from real user data.
        string? configured = Environment.GetEnvironmentVariable("FERRUM_HOME");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured!);

        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(profile))
            throw new InvalidOperationException("Cannot determine the user profile directory for .config/Ferrum.");

        return Path.Combine(profile, ".config", "Ferrum");
    }
}
