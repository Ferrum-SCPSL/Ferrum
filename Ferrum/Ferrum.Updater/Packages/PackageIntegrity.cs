using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Ferrum.Contracts;

#if FERRUM_BOOTSTRAP
namespace Ferrum.Loader.Verification;
#else
namespace Ferrum.Updater;
#endif

public static class PackageIntegrity
{
    public static string Sha256(string path)
    {
        using var algorithm = SHA256.Create(); using var stream = File.OpenRead(path);
        return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    public static string ResolveEntry(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || relative.IndexOf('\\') >= 0 || relative.IndexOf(':') >= 0 || relative.StartsWith("/", StringComparison.Ordinal)
            || relative.Split('/').Any(part => part == "." || part == ".." || part.Length == 0)) throw new InvalidDataException("Invalid package path.");
        string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(prefix, relative));
        if (!path.StartsWith(prefix, Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("Package path escapes root.");
        return path;
    }

    public static ReleaseManifest VerifyPackage(string directory, string platform, string version)
    {
        ReleaseManifest manifest = JsonFile.Read<ReleaseManifest>(Path.Combine(directory, "manifest.json"));
        ValidateMetadata(manifest, platform, version);
        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "manifest.json" };
        foreach (ManifestFile file in manifest.Files)
        {
            if (!expected.Add(file.Path) || (!file.Path.StartsWith("runtime/", StringComparison.Ordinal) && !file.Path.StartsWith("bootstrap/", StringComparison.Ordinal) && !file.Path.StartsWith("localizations/", StringComparison.Ordinal)))
                throw new InvalidDataException("Unexpected or duplicate manifest path.");
            VerifyHash(ResolveEntry(directory, file.Path), file.Sha256);
        }
        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            if (!expected.Contains(file.Substring(directory.Length + 1).Replace('\\', '/'))) throw new InvalidDataException("Unlisted package file.");
        foreach (string required in RequiredFiles(platform)) if (!expected.Contains(required)) throw new InvalidDataException("Incomplete package: " + required);
        return manifest;
    }

    public static void VerifyRuntime(string directory, string platform, string version)
    {
        ReleaseManifest manifest = JsonFile.Read<ReleaseManifest>(Path.Combine(directory, "manifest.json"));
        ValidateMetadata(manifest, platform, version);
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ManifestFile file in manifest.Files.Where(file => file.Path.StartsWith("runtime/", StringComparison.Ordinal)))
        {
            if (!files.Add(file.Path)) throw new InvalidDataException("Duplicate runtime file.");
            VerifyHash(ResolveEntry(directory, file.Path.Substring(8)), file.Sha256);
        }
        foreach (string required in RequiredFiles(platform).Where(path => path.StartsWith("runtime/", StringComparison.Ordinal)))
            if (!files.Contains(required)) throw new InvalidDataException("Incomplete runtime.");
    }

    public static void ValidateMetadata(ReleaseManifest manifest, string platform, string version)
    {
        if (manifest.Platform != platform || manifest.Version != ReleaseVersion.Parse(version).Value || manifest.Abi != 2
            || manifest.Bootstrap < 1 || manifest.Contracts < 1 || manifest.Files == null || manifest.Files.Count > 64)
            throw new InvalidDataException("Incompatible release manifest.");
        if (manifest.Branch != "alpha" && manifest.Branch != "beta" && manifest.Branch != "stable") throw new InvalidDataException("Invalid branch.");
    }

    public static void VerifyHash(string path, string expected)
    {
        if (expected == null || expected.Length != 64 || !string.Equals(Sha256(path), expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SHA-256 mismatch: " + Path.GetFileName(path));
    }

    public static IEnumerable<string> RequiredFiles(string platform)
    {
        yield return "bootstrap/Ferrum.Loader.dll"; yield return "bootstrap/Ferrum.Contracts.dll";
        foreach (string assembly in new[] { "Ferrum.Runtime", "Ferrum.Interop", "Ferrum.Localization", "Ferrum.Updater" }) yield return "runtime/" + assembly + ".dll";
        yield return "runtime/" + (platform == "win-x64" ? "ferrum_native.dll" : "libferrum_native.so");
        foreach (string language in new[] { "en", "ru", "uk", "zh-CN" }) yield return "localizations/" + language + ".csv";
    }
}
