// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The reference graph of the <c>dotnet.layout</c> deviation in <c>AGENTS.md</c>:
/// inside a service the arrow points inwards, services never reference each
/// other, and only hosts are wired together. Tools and tests are unrestricted.
/// Every rule returns its offenders as <c>project -&gt; reference</c>, so an
/// empty result means it holds.
/// </summary>
internal static class ReferenceRules
{
    /// <summary>
    /// Project and package references of a Domain project beyond
    /// <paramref name="exceptions"/>.
    /// </summary>
    internal static List<string> DomainReferences(
        IReadOnlyList<ProjectFile> projects,
        IReadOnlyCollection<string> exceptions)
    {
        return projects
            .Where(project => project.Layer == ProjectLayer.Domain)
            .SelectMany(project => project.ProjectReferences
                .Concat(project.PackageReferences)
                .Except(exceptions)
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();
    }

    /// <summary>
    /// References of a service or module project that break the layer rule:
    /// Application may reference its own Domain, Infrastructure its own
    /// Application and Domain, a host its own inner layers. All but Domain may
    /// reference <c>src/Shared/</c> and <c>src/Sdk/</c>. References to another
    /// service are left to <see cref="ReferencesBetweenUnits"/>.
    /// </summary>
    internal static List<string> ReferencesInsideAUnit(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.BelongsToUnit
                && project.Layer is not (ProjectLayer.Domain or ProjectLayer.Unknown),
            (project, target) => target.Location is ProjectLocation.Shared or ProjectLocation.Sdk
                || (target.BelongsToUnit && !project.SharesUnitWith(target))
                || (project.SharesUnitWith(target) && MayReference(project.Layer, target.Layer)));
    }

    /// <summary>References from a service or module to a project of another one.</summary>
    internal static List<string> ReferencesBetweenUnits(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.BelongsToUnit,
            (project, target) => !target.BelongsToUnit || project.SharesUnitWith(target));
    }

    /// <summary>References from <c>src/Shared/</c> or <c>src/Sdk/</c> to a service or module.</summary>
    internal static List<string> SharedReferences(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Location is ProjectLocation.Shared or ProjectLocation.Sdk,
            (_, target) => !target.BelongsToUnit);
    }

    /// <summary>
    /// References from <c>src/Aspire/</c> to anything but a host,
    /// <c>src/Shared/</c> or another Aspire project.
    /// </summary>
    internal static List<string> AspireReferences(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Location == ProjectLocation.Aspire,
            (_, target) => target.IsHost
                || target.Location is ProjectLocation.Shared or ProjectLocation.Aspire);
    }

    /// <summary>
    /// Project references to a project that is not under <c>src/</c>, from any
    /// project but a tool or a test.
    /// </summary>
    internal static List<string> ReferencesOutsideSource(IReadOnlyList<ProjectFile> projects)
    {
        var names = projects.Select(project => project.Name).ToHashSet();

        return projects
            .Where(project => project.Location is not (ProjectLocation.Tools or ProjectLocation.Tests))
            .SelectMany(project => project.ProjectReferences
                .Where(reference => !names.Contains(reference))
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();
    }

    private static bool MayReference(ProjectLayer from, ProjectLayer to)
    {
        return from switch
        {
            ProjectLayer.Application => to is ProjectLayer.Domain,
            ProjectLayer.Infrastructure => to is ProjectLayer.Domain or ProjectLayer.Application,
            ProjectLayer.Api or ProjectLayer.Worker =>
                to is ProjectLayer.Domain or ProjectLayer.Application or ProjectLayer.Infrastructure,
            _ => false,
        };
    }

    /// <summary>
    /// Every project reference, from a project <paramref name="from"/> selects,
    /// that <paramref name="allowed"/> rejects. A reference outside <c>src/</c>
    /// is left to <see cref="ReferencesOutsideSource"/>.
    /// </summary>
    private static List<string> Offenders(
        IReadOnlyList<ProjectFile> projects,
        Func<ProjectFile, bool> from,
        Func<ProjectFile, ProjectFile, bool> allowed)
    {
        var byName = projects.ToDictionary(project => project.Name);

        return projects
            .Where(from)
            .SelectMany(project => project.ProjectReferences
                .Where(reference => byName.TryGetValue(reference, out var target) && !allowed(project, target))
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();
    }
}
