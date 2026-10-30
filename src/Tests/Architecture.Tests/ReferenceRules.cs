// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The reference graph of <c>dotnet.layout</c> as the <i>Architecture</i>
/// section of <c>AGENTS.md</c> narrows it: inside a service the arrow points
/// inwards, services never reference each other, and only hosts are wired
/// together. Tools and tests are unrestricted.
/// Every rule returns its offenders as <c>project -&gt; reference</c>, so an
/// empty result means it holds.
/// </summary>
internal static class ReferenceRules
{
    /// <summary>
    /// Project, package and framework references of a Domain project beyond
    /// <c>Homeport.Shared.Contracts</c> and <paramref name="exceptions"/>.
    /// </summary>
    internal static List<string> DomainReferences(
        IReadOnlyList<ProjectFile> projects,
        IReadOnlyCollection<string> exceptions)
    {
        var sharedContracts = projects
            .Where(project => project.IsSharedContracts)
            .Select(project => project.Name)
            .ToHashSet(StringComparer.Ordinal);

        return projects
            .Where(project => project.Layer == ProjectLayer.Domain)
            .SelectMany(project => project.ProjectReferences
                .Where(reference => !sharedContracts.Contains(reference))
                .Concat(project.PackageReferences)
                .Concat(project.FrameworkReferences)
                .Except(exceptions)
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();
    }

    /// <summary>
    /// Project, package and framework references of <c>Homeport.Shared.Contracts</c>,
    /// which may have none: every project takes it, so it takes nothing with it.
    /// </summary>
    internal static List<string> SharedContractsReferences(IReadOnlyList<ProjectFile> projects)
    {
        return AnyReferences(projects, project => project.IsSharedContracts);
    }

    /// <summary>
    /// Project, package and framework references of <c>Homeport.Shared.Client</c>,
    /// which may have none: a caller takes the wire types without taking
    /// anything else with them (ADR-0018).
    /// </summary>
    internal static List<string> SharedClientReferences(IReadOnlyList<ProjectFile> projects)
    {
        return AnyReferences(projects, project => project.IsSharedClient);
    }

    /// <summary>
    /// References to <c>Homeport.Shared.Client</c> from a service or module
    /// project other than Application or a host that serves an API: Domain,
    /// Infrastructure and a Worker never see the wire types (ADR-0018).
    /// </summary>
    internal static List<string> SharedClientReferrers(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.BelongsToUnit,
            (project, target) => !target.IsSharedClient
                || project.Layer is ProjectLayer.Application or ProjectLayer.Api or ProjectLayer.Single);
    }

    /// <summary>
    /// References to <c>Homeport.Shared.Internal</c> from a Domain, from
    /// <c>src/Sdk/</c> or from <c>Homeport.Shared.Client</c>: what Homeport
    /// never ships stays out of what it ships (ADR-0018).
    /// </summary>
    internal static List<string> SharedInternalReferrers(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Layer == ProjectLayer.Domain
                || project.Location == ProjectLocation.Sdk
                || project.IsSharedClient,
            (_, target) => !target.IsSharedInternal);
    }

    /// <summary>
    /// References from <c>src/Sdk/</c> to a Shared project not in
    /// <paramref name="allowed"/>. A reference outside <c>src/Shared/</c> is
    /// left to <see cref="SharedReferences"/>.
    /// </summary>
    internal static List<string> SdkSharedReferences(
        IReadOnlyList<ProjectFile> projects,
        IReadOnlyCollection<string> allowed)
    {
        return Offenders(
            projects,
            project => project.Location == ProjectLocation.Sdk,
            (_, target) => target.Location != ProjectLocation.Shared || allowed.Contains(target.Name));
    }

    /// <summary>
    /// <c>src/Sdk/</c> and every project it references directly or
    /// transitively that does not switch on the public API analyzer: all of
    /// them are shipped, so all of them list their public API (ADR-0018).
    /// </summary>
    internal static List<string> UntrackedPublicApi(IReadOnlyList<ProjectFile> projects)
    {
        var byName = projects.ToDictionary(project => project.Name);
        var shipped = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<ProjectFile>(projects.Where(project => project.Location == ProjectLocation.Sdk));

        while (pending.TryDequeue(out var project))
        {
            if (!shipped.Add(project.Name))
            {
                continue;
            }

            foreach (var reference in project.ProjectReferences)
            {
                if (byName.TryGetValue(reference, out var target))
                {
                    pending.Enqueue(target);
                }
            }
        }

        return shipped
            .Where(name => !byName[name].TracksPublicApi)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// References of a service or module project that break the layer rule:
    /// Application may reference its own Domain, Infrastructure its own
    /// Application and Domain, a host its own inner layers. All but Domain may
    /// reference <c>src/Shared/</c>; only a host may reference <c>src/Sdk/</c>.
    /// References to another service are left to <see cref="ReferencesBetweenUnits"/>.
    /// </summary>
    internal static List<string> ReferencesInsideAUnit(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.BelongsToUnit
                && project.Layer is not (ProjectLayer.Domain or ProjectLayer.Unknown),
            (project, target) => target.Location == ProjectLocation.Shared
                || (target.Location == ProjectLocation.Sdk && project.IsHost)
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

    /// <summary>
    /// References from <c>src/Shared/</c> or <c>src/Sdk/</c> to anything outside
    /// <c>src/Shared/</c>.
    /// </summary>
    internal static List<string> SharedReferences(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Location is ProjectLocation.Shared or ProjectLocation.Sdk,
            (_, target) => target.Location == ProjectLocation.Shared);
    }

    /// <summary>
    /// References from <c>src/Aspire/</c> to anything but a host or a Shared
    /// library (<c>dotnet-core.aspire</c>).
    /// </summary>
    internal static List<string> AspireReferences(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Location == ProjectLocation.Aspire,
            (_, target) => target.IsHost
                || (target.Location == ProjectLocation.Shared && !target.IsSharedContracts));
    }

    /// <summary>References to the app host from any project but a test (<c>dotnet-core.aspire</c>).</summary>
    internal static List<string> AppHostReferrers(IReadOnlyList<ProjectFile> projects)
    {
        return Offenders(
            projects,
            project => project.Location != ProjectLocation.Tests,
            (_, target) => target.Location != ProjectLocation.Aspire);
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

    /// <summary>Every project, package and framework reference of a project <paramref name="from"/> selects.</summary>
    private static List<string> AnyReferences(IReadOnlyList<ProjectFile> projects, Func<ProjectFile, bool> from)
    {
        return projects
            .Where(from)
            .SelectMany(project => project.ProjectReferences
                .Concat(project.PackageReferences)
                .Concat(project.FrameworkReferences)
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
