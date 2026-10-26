// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// <see cref="LayoutRules"/>, each rule against this repository and against a
/// fixture tree that breaks it.
/// </summary>
public sealed class LayoutTests
{
    public sealed class MisplacedSolutions
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = LayoutRules.MisplacedSolutions(SolutionTree.SolutionPaths);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Solution_Under_Src()
        {
            string[] paths = ["Homeport.slnx", "src/Homeport.slnx"];

            var offenders = LayoutRules.MisplacedSolutions(paths);

            Assert.Equal(["src/Homeport.slnx"], offenders);
        }
    }

    public sealed class HasSolutionInRoot
    {
        [Fact]
        public void Holds_In_This_Repository()
        {
            var inRoot = LayoutRules.HasSolutionInRoot(SolutionTree.SolutionPaths);

            Assert.True(inRoot);
        }

        [Fact]
        public void Fails_When_The_Only_Solution_Lies_Under_Src()
        {
            string[] paths = ["src/Homeport.slnx"];

            var inRoot = LayoutRules.HasSolutionInRoot(paths);

            Assert.False(inRoot);
        }
    }

    public sealed class MisplacedProjects
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = LayoutRules.MisplacedProjects(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Theory]
        [InlineData("Homeport.Gateway")]
        [InlineData("Platform/Homeport.Gateway")]
        [InlineData("Modules/Homeport.Modules.Echo.Api")]
        [InlineData("Shared/Contracts/Homeport.Contracts")]
        [InlineData("Platform/Gateway/Api/Homeport.Gateway.Api")]
        public void Reports_A_Project_Outside_The_Layout(string path)
        {
            ProjectFile[] projects = [Fixture.Project(path)];

            var offenders = LayoutRules.MisplacedProjects(projects);

            Assert.Equal([$"src/{path}"], offenders);
        }

        [Theory]
        [InlineData("Aspire/Homeport.AppHost")]
        [InlineData("Shared/Homeport.Contracts")]
        [InlineData("Sdk/Homeport.Module.Sdk")]
        [InlineData("Platform/Gateway/Homeport.Gateway")]
        [InlineData("Modules/Echo/Homeport.Modules.Echo.Api")]
        [InlineData("Tools/Homeport.Seed")]
        [InlineData("Tests/Architecture.Tests")]
        public void Accepts_A_Project_In_The_Layout(string path)
        {
            ProjectFile[] projects = [Fixture.Project(path)];

            var offenders = LayoutRules.MisplacedProjects(projects);

            Assert.Empty(offenders);
        }
    }

    public sealed class MisnamedProjects
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = LayoutRules.MisnamedProjects(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Theory]
        [InlineData("Platform/Gateway/Homeport.Registry.Api")]
        [InlineData("Platform/Gateway/Homeport.Gateway.Web")]
        [InlineData("Platform/Gateway/Gateway")]
        [InlineData("Modules/Echo/Homeport.Echo.Api")]
        [InlineData("Modules/Echo/Homeport.Modules.Echo")]
        public void Reports_A_Name_That_Does_Not_Fit_Its_Service_Or_Module(string path)
        {
            ProjectFile[] projects = [Fixture.Project(path)];

            var offenders = LayoutRules.MisnamedProjects(projects);

            Assert.Equal([$"src/{path}"], offenders);
        }

        [Fact]
        public void Reports_A_Directory_That_Does_Not_Carry_The_Project_Name()
        {
            ProjectFile[] projects = [new("Homeport.Contracts", "Shared/Contracts", [], [])];

            var offenders = LayoutRules.MisnamedProjects(projects);

            Assert.Equal(["src/Shared/Contracts"], offenders);
        }

        [Theory]
        [InlineData("Platform/Gateway/Homeport.Gateway")]
        [InlineData("Platform/ControlPlane/Homeport.ControlPlane.Api")]
        [InlineData("Platform/Metering/Homeport.Metering.Worker")]
        [InlineData("Modules/Echo/Homeport.Modules.Echo.Domain")]
        [InlineData("Shared/Homeport.ServiceDefaults")]
        public void Accepts_A_Name_That_Fits(string path)
        {
            ProjectFile[] projects = [Fixture.Project(path)];

            var offenders = LayoutRules.MisnamedProjects(projects);

            Assert.Empty(offenders);
        }
    }

    public sealed class IncompleteCuts
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = LayoutRules.IncompleteCuts(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Service_Missing_A_Layer()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Domain"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Application"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Api"),
            ];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Equal(["src/Platform/Gateway: Domain, Application, Api"], offenders);
        }

        [Fact]
        public void Reports_A_Service_That_Is_Both_Single_And_Cut()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Gateway/Homeport.Gateway"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Domain"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Application"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Infrastructure"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Api"),
            ];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Equal(["src/Platform/Gateway: Single, Domain, Application, Infrastructure, Api"], offenders);
        }

        [Fact]
        public void Reports_A_Module_That_Is_Not_Cut()
        {
            ProjectFile[] projects = [Fixture.Project("Modules/Echo/Homeport.Modules.Echo")];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Equal(["src/Modules/Echo: Single"], offenders);
        }

        [Fact]
        public void Reports_A_Module_Missing_A_Layer()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Application"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Worker"),
            ];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Equal(["src/Modules/Echo: Domain, Application, Infrastructure, Worker"], offenders);
        }

        [Fact]
        public void Accepts_A_Service_In_One_Project()
        {
            ProjectFile[] projects = [Fixture.Project("Platform/ControlPlane/Homeport.ControlPlane")];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Accepts_A_Cut_Service_With_A_Worker()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Metering/Homeport.Metering.Domain"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Application"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Infrastructure"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Api"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Worker"),
            ];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Accepts_A_Cut_Module_Without_A_Worker()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Application"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Api"),
            ];

            var offenders = LayoutRules.IncompleteCuts(projects);

            Assert.Empty(offenders);
        }
    }
}
