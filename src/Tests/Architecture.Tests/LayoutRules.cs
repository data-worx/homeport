// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The directory layout of the <c>dotnet.layout</c> deviation in <c>AGENTS.md</c>:
/// where a project lies, what it is called, and how a service or module is cut.
/// Every rule returns its offenders, so an empty result means it holds.
/// </summary>
internal static class LayoutRules
{
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
    /// Projects whose directory does not carry their name, and projects of a
    /// service or module not named <c>Homeport.&lt;Service&gt;[.&lt;Layer&gt;]</c> or
    /// <c>Homeport.Modules.&lt;Module&gt;.&lt;Layer&gt;</c>.
    /// </summary>
    internal static List<string> MisnamedProjects(IEnumerable<ProjectFile> projects)
    {
        return projects
            .Where(project => !IsNamedForItsPlace(project))
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
            _ => true,
        };
    }

    private static bool IsComplete(ProjectLocation location, HashSet<ProjectLayer> layers)
    {
        var isCut = layers.IsSupersetOf(Cut) && layers.IsSubsetOf(CutWithWorker);
        var isSingle = layers.SetEquals([ProjectLayer.Single]);

        return isCut || (isSingle && location == ProjectLocation.Platform);
    }
}
