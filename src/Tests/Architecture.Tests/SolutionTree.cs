// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;

namespace Architecture.Tests;

/// <summary>
/// The repository as the tests see it: its root, its solution files, and every
/// project file under <c>src/</c>, read once.
/// </summary>
internal static class SolutionTree
{
    internal static readonly string Root = FindRoot();

    internal static readonly string Source = Path.Combine(Root, "src");

    internal static readonly IReadOnlyList<ProjectFile> Projects = ReadProjects();

    /// <summary>
    /// Every solution file in the root and anywhere under <c>src/</c>, relative to
    /// the root with <c>/</c> as separator.
    /// </summary>
    internal static readonly IReadOnlyList<string> SolutionPaths = ReadSolutionPaths();

    /// <summary>
    /// Walks up from the test assembly to the directory holding <c>.git</c>. Not
    /// to the first solution file: one that wrongly sits in <c>src/</c> would be
    /// mistaken for the root, and the layout test could never see it.
    /// </summary>
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No .git found above {AppContext.BaseDirectory}; the tests need the repository checkout.");
    }

    private static List<ProjectFile> ReadProjects()
    {
        return Directory.EnumerateFiles(Source, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(Read)
            .ToList();
    }

    private static List<string> ReadSolutionPaths()
    {
        return SolutionFiles(Root, SearchOption.TopDirectoryOnly)
            .Concat(SolutionFiles(Source, SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .ToList();
    }

    private static IEnumerable<string> SolutionFiles(string directory, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(directory, "*.slnx", searchOption)
            .Concat(Directory.EnumerateFiles(directory, "*.sln", searchOption))
            .Where(path => !IsBuildOutput(path));
    }

    private static ProjectFile Read(string path)
    {
        var directory = Path.GetRelativePath(Source, Path.GetDirectoryName(path)!).Replace('\\', '/');
        var document = XDocument.Load(path);

        return new ProjectFile(
            Path.GetFileNameWithoutExtension(path),
            directory,
            Includes(document, "ProjectReference").Select(ProjectName).ToList(),
            Includes(document, "PackageReference").ToList());
    }

    /// <summary>
    /// Matched by local name, so a project file that declares the MSBuild
    /// namespace is read the same as one that does not.
    /// </summary>
    private static IEnumerable<string> Includes(XDocument document, string element)
    {
        return document.Descendants()
            .Where(node => node.Name.LocalName == element)
            .Select(node => (string?)node.Attribute("Include"))
            .OfType<string>();
    }

    private static string ProjectName(string include)
    {
        return Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
    }

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');
        return segments.Contains("bin") || segments.Contains("obj");
    }
}
