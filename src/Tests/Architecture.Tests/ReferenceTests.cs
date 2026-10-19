namespace Architecture.Tests;

/// <summary>
/// The reference graph of <c>dotnet.layout</c>: the arrow points inwards, and a
/// UI is a leaf that no other UI builds on.
/// </summary>
public sealed class ReferenceTests
{
    public sealed class Domain
    {
        [Fact]
        public void References_Nothing_But_The_Listed_Exceptions()
        {
            var offenders = SolutionTree.Projects
                .Where(project => project.IsDomain)
                .SelectMany(project => project.ProjectReferences
                    .Concat(project.PackageReferences)
                    .Except(DomainExceptions.Allowed)
                    .Select(reference => $"{project.Name} -> {reference}"))
                .ToList();

            Assert.Empty(offenders);
        }
    }

    public sealed class Application
    {
        [Fact]
        public void References_No_Project_But_Domain()
        {
            var offenders = Offenders(
                project => project.IsApplication,
                reference => reference.IsDomain);

            Assert.Empty(offenders);
        }
    }

    public sealed class Infrastructure
    {
        [Fact]
        public void References_No_Project_But_Application_And_Domain()
        {
            var offenders = Offenders(
                project => project.IsInfrastructure,
                reference => reference.IsApplication || reference.IsDomain);

            Assert.Empty(offenders);
        }
    }

    public sealed class Ui
    {
        [Fact]
        public void References_No_Other_Ui()
        {
            var offenders = Offenders(
                project => project.Location == ProjectLocation.Ui,
                reference => reference.Location != ProjectLocation.Ui);

            Assert.Empty(offenders);
        }
    }

    /// <summary>
    /// Every project reference, from a project <paramref name="from"/> selects,
    /// that <paramref name="allowed"/> rejects. A reference to a project outside
    /// <c>src/</c> is not in the tree and is never allowed.
    /// </summary>
    private static List<string> Offenders(Func<ProjectFile, bool> from, Func<ProjectFile, bool> allowed)
    {
        var byName = SolutionTree.Projects.ToDictionary(project => project.Name);

        return SolutionTree.Projects
            .Where(from)
            .SelectMany(project => project.ProjectReferences
                .Where(reference => !byName.TryGetValue(reference, out var target) || !allowed(target))
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();
    }
}
