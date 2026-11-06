using System;
using System.Globalization;
using System.Text.RegularExpressions;

#if FERRUM_BOOTSTRAP
namespace Ferrum.Loader.Verification;
#else
namespace Ferrum.Updater;
#endif

public sealed class ReleaseVersion : IComparable<ReleaseVersion>
{
    private readonly long[] core;
    private readonly string[] suffix;
    public string Value { get; }
    public bool IsPrerelease => suffix.Length != 0;
    private ReleaseVersion(string value, long[] core, string[] suffix) { Value = value; this.core = core; this.suffix = suffix; }

    public static ReleaseVersion Parse(string value)
    {
        string normalized = value.StartsWith("v", StringComparison.Ordinal) ? value.Substring(1) : value;
        Match match = Regex.Match(normalized, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$");
        if (!match.Success) throw new FormatException("Invalid semantic release version.");
        var numbers = new long[3];
        for (int i = 0; i < 3; i++) numbers[i] = long.Parse(match.Groups[i + 1].Value, CultureInfo.InvariantCulture);
        string[] pre = match.Groups[4].Success ? match.Groups[4].Value.Split('.') : Array.Empty<string>();
        foreach (string part in pre)
            if (part.Length > 1 && part[0] == '0' && Regex.IsMatch(part, @"^\d+$")) throw new FormatException("Leading zero in prerelease.");
        return new ReleaseVersion(normalized, numbers, pre);
    }

    public int CompareTo(ReleaseVersion? other)
    {
        if (other == null) return 1;
        for (int i = 0; i < 3; i++) { int difference = core[i].CompareTo(other.core[i]); if (difference != 0) return difference; }
        if (suffix.Length == 0 || other.suffix.Length == 0) return (suffix.Length == 0 ? 1 : 0).CompareTo(other.suffix.Length == 0 ? 1 : 0);
        for (int i = 0; i < Math.Min(suffix.Length, other.suffix.Length); i++)
        {
            string left = suffix[i], right = other.suffix[i];
            bool ln = Regex.IsMatch(left, @"^\d+$"), rn = Regex.IsMatch(right, @"^\d+$");
            int result = ln && rn ? left.Length.CompareTo(right.Length) : ln != rn ? (ln ? -1 : 1) : 0;
            if (result == 0) result = string.CompareOrdinal(left, right);
            if (result != 0) return result;
        }
        return suffix.Length.CompareTo(other.suffix.Length);
    }
}
