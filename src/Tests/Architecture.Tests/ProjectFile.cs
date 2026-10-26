// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>One <c>.csproj</c> under <c>src/</c>, with what it references.</summary>
/// <param name="Solution">The name of the solution, which every project name starts with.</param>
/// <param name="Name">The file name without its extension.</param>
/// <param name="RelativeDirectory">The directory, relative to <c>src/</c>, with <c>/</c> as separator.</param>
/// <param name="ProjectReferences">The names of the projects it references.</param>
/// <param name="PackageReferences">The names of the packages it references.</param>
internal sealed record ProjectFile(
    string Solution,
    string Name,
    string RelativeDirectory,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences)
{
    internal string Path => $"src/{RelativeDirectory}";

    internal string DirectoryName => Segments[^1];

    internal ProjectLocation Location => Segments switch
    {
        ["Aspire", _] => ProjectLocation.Aspire,
        ["Shared", _] => ProjectLocation.Shared,
        ["Sdk", _] => ProjectLocation.Sdk,
        ["Platform", _, _] => ProjectLocation.Platform,
        ["Modules", _, _] => ProjectLocation.Modules,
        ["Tools", _] => ProjectLocation.Tools,
        ["Tests", _] => ProjectLocation.Tests,
        _ => ProjectLocation.Elsewhere,
    };

    /// <summary>The service or module the project belongs to, or <c>null</c>.</summary>
    internal string? Unit => BelongsToUnit ? Segments[1] : null;

    internal ProjectLayer Layer => BelongsToUnit ? LayerAfter(GroupingName) : ProjectLayer.None;

    /// <summary>
    /// The solution name followed by every grouping directory between <c>src/</c>
    /// and the project, such as <c>Homeport.Platform.Gateway</c>. A project's name
    /// is this or starts with it and a dot (<c>dotnet.layout</c>).
    /// </summary>
    internal string GroupingName => string.Join('.', [Solution, .. Segments[..^1]]);

    /// <summary>
    /// The project a test project is named after, or <c>null</c> when the name
    /// ends in neither <c>.Tests.Unit</c> nor <c>.Tests.Integration</c>
    /// (<c>dotnet.tests</c>).
    /// </summary>
    internal string? ProjectUnderTest
    {
        get
        {
            string[] suffixes = [".Tests.Unit", ".Tests.Integration"];
            var suffix = suffixes.FirstOrDefault(candidate => Name.EndsWith(candidate, StringComparison.Ordinal));
            return suffix is null ? null : Name[..^suffix.Length];
        }
    }

    internal bool BelongsToUnit => Location is ProjectLocation.Platform or ProjectLocation.Modules;

    /// <summary>A deployable: a single-project service, an Api or a Worker.</summary>
    internal bool IsHost => Layer is ProjectLayer.Single or ProjectLayer.Api or ProjectLayer.Worker;

    internal bool SharesUnitWith(ProjectFile other)
    {
        return BelongsToUnit && Location == other.Location && Unit == other.Unit;
    }

    private string[] Segments => RelativeDirectory.Split('/');

    private ProjectLayer LayerAfter(string unitName)
    {
        if (Name == unitName)
        {
            return ProjectLayer.Single;
        }

        if (!Name.StartsWith($"{unitName}.", StringComparison.Ordinal))
        {
            return ProjectLayer.Unknown;
        }

        return Name[(unitName.Length + 1)..] switch
        {
            "Domain" => ProjectLayer.Domain,
            "Application" => ProjectLayer.Application,
            "Infrastructure" => ProjectLayer.Infrastructure,
            "Api" => ProjectLayer.Api,
            "Worker" => ProjectLayer.Worker,
            _ => ProjectLayer.Unknown,
        };
    }
}
