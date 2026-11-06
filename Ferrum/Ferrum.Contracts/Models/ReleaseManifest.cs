using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ferrum.Contracts;

[DataContract]
public sealed class ReleaseManifest
{
    [DataMember(Name = "version", IsRequired = true)] public string Version { get; set; } = string.Empty;
    [DataMember(Name = "platform", IsRequired = true)] public string Platform { get; set; } = string.Empty;
    [DataMember(Name = "branch", IsRequired = true)] public string Branch { get; set; } = "alpha";
    [DataMember(Name = "bootstrap", IsRequired = true)] public int Bootstrap { get; set; }
    [DataMember(Name = "contracts", IsRequired = true)] public int Contracts { get; set; }
    [DataMember(Name = "abi", IsRequired = true)] public int Abi { get; set; }
    [DataMember(Name = "files", IsRequired = true)] public List<ManifestFile> Files { get; set; } = new();
}

[DataContract]
public sealed class ManifestFile
{
    [DataMember(Name = "path", IsRequired = true)] public string Path { get; set; } = string.Empty;
    [DataMember(Name = "sha256", IsRequired = true)] public string Sha256 { get; set; } = string.Empty;
}

[DataContract]
public sealed class RuntimeState
{
    [DataMember(Name = "active")] public string Active { get; set; } = string.Empty;
    [DataMember(Name = "previous")] public string Previous { get; set; } = string.Empty;
    [DataMember(Name = "pending")] public string Pending { get; set; } = string.Empty;
    [DataMember(Name = "attempt")] public string Attempt { get; set; } = string.Empty;
    [DataMember(Name = "rejected")] public string Rejected { get; set; } = string.Empty;
}
