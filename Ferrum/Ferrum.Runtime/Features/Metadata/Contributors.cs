using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ferrum.Runtime.Features.Metadata;

/// <summary>Contributor names compiled from the repository's CONTRIBUTORS.txt.</summary>
public static class Contributors
{
    public static IReadOnlyList<string> Names { get; } = LoadNames();

    private static IReadOnlyList<string> LoadNames()
    {
        using Stream stream = typeof(Contributors).Assembly.GetManifestResourceStream("Ferrum.Runtime.Contributors")
            ?? throw new InvalidOperationException("The embedded Ferrum contributor list is missing.");
        using var reader = new StreamReader(stream);
        string[] names = reader.ReadToEnd()
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => name.Length != 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Array.AsReadOnly(names);
    }
}
