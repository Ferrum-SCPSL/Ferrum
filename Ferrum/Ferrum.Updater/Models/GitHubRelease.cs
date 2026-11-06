using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ferrum.Updater;

[DataContract]
public sealed class GitHubRelease
{
    [DataMember(Name = "tag_name")] public string Tag { get; set; } = string.Empty;
    [DataMember(Name = "draft")] public bool Draft { get; set; }
    [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
    [DataMember(Name = "assets")] public List<GitHubAsset> Assets { get; set; } = new();
}

[DataContract]
public sealed class GitHubAsset
{
    [DataMember(Name = "name")] public string Name { get; set; } = string.Empty;
    [DataMember(Name = "browser_download_url")] public string DownloadUrl { get; set; } = string.Empty;
}
