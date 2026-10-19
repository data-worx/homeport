namespace Architecture.Tests;

/// <summary>
/// The directory layout of <c>dotnet.layout</c>: the solution in the root, the
/// application in <c>src/Core/</c>, every UI directly under <c>src/</c>.
/// </summary>
public sealed class LayoutTests
{
    public sealed class Solution
    {
        [Fact]
        public void Lies_In_The_Repository_Root()
        {
            var inRoot = SolutionTree.SolutionFiles(SolutionTree.Root, SearchOption.TopDirectoryOnly);

            Assert.NotEmpty(inRoot);
        }

        [Fact]
        public void Has_No_Copy_Under_Src()
        {
            var underSource = SolutionTree.SolutionFiles(SolutionTree.Source, SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(SolutionTree.Root, path))
                .ToList();

            Assert.Empty(underSource);
        }
    }

    public sealed class Projects
    {
        [Fact]
        public void Lie_In_Core_Tools_Tests_Or_Directly_Under_Src()
        {
            var offenders = SolutionTree.Projects
                .Where(project => project.Location == ProjectLocation.Elsewhere)
                .Select(project => $"src/{project.RelativeDirectory}")
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void Put_Every_Layer_In_Core()
        {
            var offenders = SolutionTree.Projects
                .Where(project => project.IsLayer && project.Location != ProjectLocation.Core)
                .Select(project => $"src/{project.RelativeDirectory}")
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void Keep_Anything_But_Layers_Out_Of_Core()
        {
            var offenders = SolutionTree.Projects
                .Where(project => project.Location == ProjectLocation.Core && !project.IsLayer)
                .Select(project => $"src/{project.RelativeDirectory}")
                .ToList();

            Assert.Empty(offenders);
        }
    }
}
