using System.Linq;
using System.Reflection;

namespace Ferrum.Runtime.Features.Metadata;

/// <summary>Release metadata taken from the loader assembly, without separate version constants.</summary>
public static class ReleaseInfo
{
    private static readonly Assembly Assembly = typeof(ReleaseInfo).Assembly;

    public static string Version => Assembly.GetName().Version?.ToString(3) ?? "unknown";

    public static string Branch => Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), inherit: false)
        .OfType<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == "Ferrum.ReleaseBranch")?.Value ?? "alpha";
}
