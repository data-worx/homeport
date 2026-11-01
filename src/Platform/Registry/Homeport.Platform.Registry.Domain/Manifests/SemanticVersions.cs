// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;

namespace Homeport.Platform.Registry.Domain.Manifests;

/// <summary>
/// Semantic Versioning 2.0.0 on plain strings: which strings are versions,
/// and which of two versions takes precedence.
/// </summary>
public static partial class SemanticVersions
{
    /// <summary>
    /// Orders versions by precedence, ignoring build metadata.
    /// </summary>
    /// <exception cref="ArgumentException">A compared string is not a semantic version.</exception>
    public static IComparer<string> Precedence { get; } = Comparer<string>.Create(Compare);

    /// <summary>Whether <paramref name="version"/> is a semantic version.</summary>
    public static bool IsValid(string version)
    {
        return VersionPattern().IsMatch(version);
    }

    private static int Compare(string? left, string? right)
    {
        var a = Parse(left);
        var b = Parse(right);

        var result = CompareNumeric(a.Major, b.Major);
        result = result != 0 ? result : CompareNumeric(a.Minor, b.Minor);
        result = result != 0 ? result : CompareNumeric(a.Patch, b.Patch);

        return result != 0 ? result : ComparePreRelease(a.PreRelease, b.PreRelease);
    }

    // A version without a pre-release ranks above one with it; otherwise the
    // identifiers decide from left to right, and more identifiers rank higher.
    private static int ComparePreRelease(string[] left, string[] right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return Math.Sign(right.Length.CompareTo(left.Length));
        }

        for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
        {
            var result = CompareIdentifier(left[index], right[index]);
            if (result != 0)
            {
                return result;
            }
        }

        return Math.Sign(left.Length.CompareTo(right.Length));
    }

    // Numeric identifiers compare numerically and rank below alphanumeric ones,
    // which compare in ASCII order.
    private static int CompareIdentifier(string left, string right)
    {
        var leftIsNumeric = left.All(char.IsAsciiDigit);
        var rightIsNumeric = right.All(char.IsAsciiDigit);

        return (leftIsNumeric, rightIsNumeric) switch
        {
            (true, true) => CompareNumeric(left, right),
            (true, false) => -1,
            (false, true) => 1,
            _ => Math.Sign(string.CompareOrdinal(left, right)),
        };
    }

    // Without leading zeros the longer number is the larger one, so numbers of
    // any length compare without parsing.
    private static int CompareNumeric(string left, string right)
    {
        var byLength = left.Length.CompareTo(right.Length);
        return Math.Sign(byLength != 0 ? byLength : string.CompareOrdinal(left, right));
    }

    private static Parts Parse(string? version)
    {
        var match = VersionPattern().Match(version ?? string.Empty);
        if (!match.Success)
        {
            throw new ArgumentException($"'{version}' is not a semantic version.", nameof(version));
        }

        var preRelease = match.Groups["pre"].Success ? match.Groups["pre"].Value.Split('.') : [];

        return new Parts(match.Groups["major"].Value, match.Groups["minor"].Value, match.Groups["patch"].Value, preRelease);
    }

    // The pattern semver.org recommends, with named groups.
    [GeneratedRegex(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)"
        + @"(?:-(?<pre>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?"
        + @"(?:\+[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*)?$")]
    private static partial Regex VersionPattern();

    private sealed record Parts(string Major, string Minor, string Patch, string[] PreRelease);
}
