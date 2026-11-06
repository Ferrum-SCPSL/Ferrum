using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ferrum.Contracts;

namespace Ferrum.Updater;

public sealed class PreparedPackage : IDisposable
{
    private readonly string workingDirectory;
    public string DirectoryPath { get; }
    public ReleaseManifest Manifest { get; }
    private PreparedPackage(string work, string payload, ReleaseManifest manifest) { workingDirectory = work; DirectoryPath = payload; Manifest = manifest; }

    public static async Task<PreparedPackage> DownloadAsync(GitHubClient client, GitHubRelease release, string platform, string stagingRoot, CancellationToken token)
    {
        string version = ReleaseVersion.Parse(release.Tag).Value;
        string work = Path.Combine(stagingRoot, "download-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(work);
        try
        {
            string name = platform == "win-x64" ? "Ferrum-win-x64.zip" : "Ferrum-linux-x64.tar.gz";
            string sums = await client.AssetTextAsync(release, "SHA256SUMS.txt", token).ConfigureAwait(false);
            string? expected = sums.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length == 2 && parts[1].TrimStart('*') == name).Select(parts => parts[0]).SingleOrDefault();
            if (expected == null) throw new InvalidDataException("Package checksum missing.");
            string archive = Path.Combine(work, name);
            await client.DownloadAsync(release, name, archive, token).ConfigureAwait(false);
            PackageIntegrity.VerifyHash(archive, expected);
            string payload = Path.Combine(work, "payload"); SafeArchive.Extract(archive, payload, platform == "linux-x64");
            ReleaseManifest manifest = PackageIntegrity.VerifyPackage(payload, platform, version);
            return new PreparedPackage(work, payload, manifest);
        }
        catch { Directory.Delete(work, true); throw; }
    }

    /// <summary>Caller holds the Ferrum-root install lock. Existing immutable versions are only verified.</summary>
    public string InstallRuntime(string root)
    {
        string destination = InstallPaths.Runtime(root, Manifest.Version, Manifest.Platform);
        if (Directory.Exists(destination)) { PackageIntegrity.VerifyRuntime(destination, Manifest.Platform, Manifest.Version); return destination; }
        string stage = Path.Combine(root, "Runtime", ".staging-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
        try
        {
            foreach (ManifestFile file in Manifest.Files.Where(file => file.Path.StartsWith("runtime/", StringComparison.Ordinal)))
                File.Copy(PackageIntegrity.ResolveEntry(DirectoryPath, file.Path), PackageIntegrity.ResolveEntry(stage, file.Path.Substring(8)));
            File.Copy(Path.Combine(DirectoryPath, "manifest.json"), Path.Combine(stage, "manifest.json"));
            PackageIntegrity.VerifyRuntime(stage, Manifest.Platform, Manifest.Version);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); Directory.Move(stage, destination);
            return destination;
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }

    public void Dispose() { if (Directory.Exists(workingDirectory)) Directory.Delete(workingDirectory, true); }
}
