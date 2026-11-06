using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ferrum.Contracts;

namespace Ferrum.Updater;

/// <summary>Unauthenticated public release downloads from the one official repository.</summary>
public sealed class GitHubClient : IDisposable
{
    public const string Repository = "Ferrum-SCPSL/Ferrum";
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(480) };
    public GitHubClient() { client.DefaultRequestHeaders.UserAgent.ParseAdd("Ferrum/0.1.0"); client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json"); }

    public async Task<GitHubRelease?> FindReleaseAsync(string requested, string channel, CancellationToken token)
    {
        if (requested != "latest")
        {
            string version = ReleaseVersion.Parse(requested).Value;
            string text = await GetTextAsync("https://api.github.com/repos/" + Repository + "/releases/tags/v" + version, token).ConfigureAwait(false);
            GitHubRelease release = JsonFile.Parse<GitHubRelease>(text);
            return release.Draft ? null : release;
        }
        var candidates = new List<GitHubRelease>();
        for (int page = 1; page <= 10; page++)
        {
            string text = await GetTextAsync("https://api.github.com/repos/" + Repository + "/releases?per_page=100&page=" + page, token).ConfigureAwait(false);
            GitHubRelease[] releases = JsonFile.Parse<GitHubRelease[]>(text);
            foreach (GitHubRelease release in releases)
            {
                if (release.Draft) continue;
                try
                {
                    ReleaseVersion version = ReleaseVersion.Parse(release.Tag);
                    if (channel == "stable" && (release.Prerelease || version.IsPrerelease)) continue;
                    candidates.Add(release);
                }
                catch (FormatException) { }
                catch (OverflowException) { }
            }
            if (releases.Length < 100) break;
        }
        return candidates.OrderByDescending(release => ReleaseVersion.Parse(release.Tag)).FirstOrDefault();
    }

    public Task<string> AssetTextAsync(GitHubRelease release, string name, CancellationToken token) => GetTextAsync(AssetUrl(release, name), token);

    public async Task DownloadAsync(GitHubRelease release, string name, string path, CancellationToken token)
    {
        using var response = await client.GetAsync(AssetUrl(release, name), HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 128 * 1024 * 1024) throw new InvalidDataException("Package too large.");
        using Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[81920]; long size = 0; int count;
        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
        {
            size += count; if (size > 128 * 1024 * 1024) throw new InvalidDataException("Package too large.");
            await output.WriteAsync(buffer, 0, count, token).ConfigureAwait(false);
        }
    }

    private async Task<string> GetTextAsync(string url, CancellationToken token)
    {
        using var response = await client.GetAsync(url, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (text.Length > 4 * 1024 * 1024) throw new InvalidDataException("Response too large.");
        return text;
    }

    private static string AssetUrl(GitHubRelease release, string name)
    {
        string prefix = "https://github.com/" + Repository + "/releases/download/" + release.Tag + "/";
        GitHubAsset asset = release.Assets.Single(asset => asset.Name == name);
        if (!asset.DownloadUrl.StartsWith(prefix, StringComparison.Ordinal) || !Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out _))
            throw new InvalidDataException("Unexpected release asset URL.");
        return asset.DownloadUrl;
    }
    public void Dispose() => client.Dispose();
}
