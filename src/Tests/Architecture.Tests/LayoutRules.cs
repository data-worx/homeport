// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The directory layout of <c>dotnet.layout</c> as the <i>Architecture</i>
/// section of <c>AGENTS.md</c> narrows it: where a project lies, what it is
/// called, and how a service or module is cut.
/// Every rule returns its offenders, so an empty result means it holds.
/// </summary>
internal static class LayoutRules
{
    /// <summary>The app host's name after its grouping directory (<c>dotnet-core.aspire</c>).</summary>
    private const string AppHostSuffix = ".AppHost";

    private static readonly ProjectLayer[] Cut =
        [ProjectLayer.Domain, ProjectLayer.Application, ProjectLayer.Infrastructure, ProjectLayer.Api];

    private static readonly ProjectLayer[] CutWithWorker = [.. Cut, ProjectLayer.Worker];

    /// <summary>Solution files anywhere but the repository root.</summary>
    /// <param name="solutionPaths">Relative to the root, with <c>/</c> as separator.</param>
    internal static List<string> MisplacedSolutions(IEnumerable<string> solutionPaths)
    {
        return solutionPaths.Where(path => path.Contains('/')).ToList();
    }

    /// <summary>Whether a solution file lies in the repository root.</summary>
    /// <param name="solutionPaths">Relative to the root, with <c>/</c> as separator.</param>
    internal static bool HasSolutionInRoot(IEnumerable<string> solutionPaths)
    {
        return solutionPaths.Any(path => !path.Contains('/'));
    }

    /// <summary>
    /// Projects outside <c>src/Aspire/</c>, <c>src/Shared/</c>, <c>src/Sdk/</c>,
    /// <c>src/Platform/&lt;Service&gt;/</c>, <c>src/Modules/&lt;Module&gt;/</c>,
    /// <c>src/Tools/</c> and <c>src/Tests/</c>.
    /// </summary>
    internal static List<string> MisplacedProjects(IEnumerable<ProjectFile> projects)
    {
        return projects
            .Where(project => project.Location == ProjectLocation.Elsewhere)
            .Select(project => project.Path)
            .ToList();
    }

    /// <summary>
    /// Projects whose directory does not carry their name, and projects outside
    /// <c>src/Tests/</c> whose name is not the solution name followed by their
    /// grouping directories: <c>Homeport.Shared[.&lt;Name&gt;]</c>,
    /// <c>Homeport.Platform.&lt;Service&gt;[.&lt;Layer&gt;]</c>,
    /// <c>Homeport.Modules.&lt;Module&gt;.&lt;Layer&gt;</c>. <c>src/Sdk/</c> holds
    /// only <c>Homeport.Sdk</c>, <c>src/Aspire/</c> only <c>Homeport.Aspire.AppHost</c>.
    /// </summary>
    internal static List<string> MisnamedProjects(IEnumerable<ProjectFile> projects)
    {
        return projects
            .Where(project => !IsNamedForItsPlace(project))
            .Select(project => project.Path)
            .ToList();
    }

    /// <summary>
    /// Projects outside a service or module that carry a layer name, such as
    /// <c>Homeport.Shared.Billing.Domain</c>: Shared holds no business logic, and
    /// a layer belongs to the service or module it serves. Tests are left out.
    /// </summary>
    internal static List<string> LayersOutsideAUnit(IEnumerable<ProjectFile> projects)
    {
        return projects
            .Where(project => !project.BelongsToUnit && project.Location != ProjectLocation.Tests)
            .Where(project => project.CarriesLayerName)
            .Select(project => project.Path)
            .ToList();
    }

    /// <summary>
    /// Test projects not named after a project of the tree plus <c>.Tests.Unit</c>
    /// or <c>.Tests.Integration</c>. <c>Architecture.Tests</c> is the one
    /// solution-wide exception (<c>dotnet.tests</c>).
    /// </summary>
    internal static List<string> MisnamedTestProjects(IReadOnlyList<ProjectFile> projects)
    {
        var production = projects
            .Where(project => project.Location != ProjectLocation.Tests)
            .Select(project => project.Name)
            .ToHashSet(StringComparer.Ordinal);

        return projects
            .Where(project => project.Location == ProjectLocation.Tests && project.Name != "Architecture.Tests")
            .Where(project => project.ProjectUnderTest is not { } underTest || !production.Contains(underTest))
            .Select(project => project.Path)
            .ToList();
    }

    /// <summary>
    /// Services that are neither one project nor cut into Domain, Application,
    /// Infrastructure and Api, and modules that are not cut. Either may add a
    /// Worker to the cut. A misnamed project is left to <see cref="MisnamedProjects"/>.
    /// </summary>
    internal static List<string> IncompleteCuts(IEnumerable<ProjectFile> projects)
    {
        return projects
            .Where(project => project.BelongsToUnit && project.Layer != ProjectLayer.Unknown)
            .GroupBy(project => (project.Location, project.Unit))
            .Where(unit => !IsComplete(unit.Key.Location, unit.Select(project => project.Layer).ToHashSet()))
            .Select(unit => $"src/{unit.Key.Location}/{unit.Key.Unit}: "
                + string.Join(", ", unit.Select(project => project.Layer).Order()))
            .ToList();
    }

    private static bool IsNamedForItsPlace(ProjectFile project)
    {
        if (project.DirectoryName != project.Name)
        {
            return false;
        }

        return project.Location switch
        {
            ProjectLocation.Platform => project.Layer != ProjectLayer.Unknown,
            ProjectLocation.Modules => project.Layer is not (ProjectLayer.Unknown or ProjectLayer.Single),
            ProjectLocation.Sdk => project.Name == project.GroupingName,
            ProjectLocation.Aspire => project.Name == $"{project.GroupingName}{AppHostSuffix}",
            ProjectLocation.Tests or ProjectLocation.Elsewhere => true,
            _ => project.Name == project.GroupingName
                || project.Name.StartsWith($"{project.GroupingName}.", StringComparison.Ordinal),
        };
    }

    private static bool IsComplete(ProjectLocation location, HashSet<ProjectLayer> layers)
    {
        var isCut = layers.IsSupersetOf(Cut) && layers.IsSubsetOf(CutWithWorker);
        var isSingle = layers.SetEquals([ProjectLayer.Single]);

        return isCut || (isSingle && location == ProjectLocation.Platform);
    }
}
