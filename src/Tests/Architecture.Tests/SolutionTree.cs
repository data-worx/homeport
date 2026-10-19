using System.Xml.Linq;

namespace Architecture.Tests;

/// <summary>
/// The repository as the tests see it: its root, and every project file under
/// <c>src/</c>, read once.
/// </summary>
internal static class SolutionTree
{
    internal static readonly string Root = FindRoot();

    internal static readonly string Source = Path.Combine(Root, "src");

    internal static readonly IReadOnlyList<ProjectFile> Projects = ReadProjects();

    internal static IEnumerable<string> SolutionFiles(string directory, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(directory, "*.slnx", searchOption)
            .Concat(Directory.EnumerateFiles(directory, "*.sln", searchOption))
            .Where(path => !IsBuildOutput(path));
    }

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

    private static ProjectFile Read(string path)
    {
        var directory = Path.GetRelativePath(Source, Path.GetDirectoryName(path)!).Replace('\\', '/');
        var document = XDocument.Load(path);

        return new ProjectFile(
            Path.GetFileNameWithoutExtension(path),
            directory,
            LocationOf(directory),
            Includes(document, "ProjectReference").Select(ProjectName).ToList(),
            Includes(document, "PackageReference").ToList());
    }

    private static ProjectLocation LocationOf(string relativeDirectory)
    {
        var segments = relativeDirectory.Split('/');
        if (segments.Length == 1)
        {
            return ProjectLocation.Ui;
        }

        if (segments.Length != 2)
        {
            return ProjectLocation.Elsewhere;
        }

        return segments[0] switch
        {
            "Core" => ProjectLocation.Core,
            "Tools" => ProjectLocation.Tools,
            "Tests" => ProjectLocation.Tests,
            _ => ProjectLocation.Elsewhere,
        };
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
