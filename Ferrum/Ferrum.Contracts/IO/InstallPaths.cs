using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Ferrum.Contracts;

public static class InstallPaths
{
    public static string Platform => IntPtr.Size != 8 ? throw new PlatformNotSupportedException("x64 required.")
        : Environment.OSVersion.Platform == PlatformID.Win32NT ? "win-x64"
        : Environment.OSVersion.Platform == PlatformID.Unix ? "linux-x64"
        : throw new PlatformNotSupportedException("Unsupported platform.");

    public static string ResolveRoot(string? configured)
    {
        string? path = Environment.GetEnvironmentVariable("FERRUM_HOME");
        if (string.IsNullOrWhiteSpace(path)) path = configured;
        if (string.IsNullOrWhiteSpace(path))
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(profile)) throw new DirectoryNotFoundException("User profile unavailable.");
            path = Path.Combine(profile, ".config", "Ferrum");
        }
        if (!Path.IsPathRooted(path!)) throw new ArgumentException("Ferrum root must be an absolute path.");
        return Path.GetFullPath(path!);
    }

    public static string Runtime(string root, string version, string platform)
    {
        if (!Regex.IsMatch(version ?? "", @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$"))
            throw new InvalidDataException("Invalid release version.");
        if (platform != "win-x64" && platform != "linux-x64") throw new InvalidDataException("Invalid platform.");
        return Path.Combine(root, "Runtime", version!, platform);
    }

    public static string StateDirectory(string root, string labApiPath, int port)
    {
        string canonical = Path.GetFullPath(labApiPath).TrimEnd(Path.DirectorySeparatorChar);
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) canonical = canonical.ToUpperInvariant();
        using var sha = SHA256.Create();
        string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "").Substring(0, 16);
        return Path.Combine(root, "Updates", id + "-" + port);
    }

    public static void EnsureDataDirectories(string root)
    {
        foreach (string relative in new[] { "Configs", "Logs", "Plugins", "Plugins/Dependencies", "Localizations", "Runtime", "Updates" })
            Directory.CreateDirectory(Path.Combine(root, relative));
    }
}
