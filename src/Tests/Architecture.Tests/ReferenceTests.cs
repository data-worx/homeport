// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// <see cref="ReferenceRules"/>, each rule against this repository and against a
/// fixture tree that breaks it.
/// </summary>
public sealed class ReferenceTests
{
    public sealed class DomainReferences
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.DomainReferences(SolutionTree.Projects, DomainExceptions.Allowed);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Project_Reference()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain", "Homeport.Contracts"),
                Fixture.Project("Shared/Homeport.Contracts"),
            ];

            var offenders = ReferenceRules.DomainReferences(projects, []);

            Assert.Equal(["Homeport.Modules.Echo.Domain -> Homeport.Contracts"], offenders);
        }

        [Fact]
        public void Reports_A_Package_Reference()
        {
            ProjectFile[] projects =
            [
                Fixture.WithPackages("Modules/Echo/Homeport.Modules.Echo.Domain", "Microsoft.EntityFrameworkCore"),
            ];

            var offenders = ReferenceRules.DomainReferences(projects, []);

            Assert.Equal(["Homeport.Modules.Echo.Domain -> Microsoft.EntityFrameworkCore"], offenders);
        }

        [Fact]
        public void Accepts_A_Listed_Exception()
        {
            ProjectFile[] projects =
            [
                Fixture.WithPackages("Modules/Echo/Homeport.Modules.Echo.Domain", "Acme.Rules"),
            ];

            var offenders = ReferenceRules.DomainReferences(projects, ["Acme.Rules"]);

            Assert.Empty(offenders);
        }
    }

    public sealed class ReferencesInsideAUnit
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.ReferencesInsideAUnit(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Theory]
        [InlineData("Application", "Infrastructure")]
        [InlineData("Application", "Api")]
        [InlineData("Infrastructure", "Api")]
        [InlineData("Api", "Worker")]
        [InlineData("Worker", "Api")]
        public void Reports_A_Reference_Against_The_Arrow(string from, string to)
        {
            ProjectFile[] projects =
            [
                Fixture.Project($"Modules/Echo/Homeport.Modules.Echo.{from}", $"Homeport.Modules.Echo.{to}"),
                Fixture.Project($"Modules/Echo/Homeport.Modules.Echo.{to}"),
            ];

            var offenders = ReferenceRules.ReferencesInsideAUnit(projects);

            Assert.Equal([$"Homeport.Modules.Echo.{from} -> Homeport.Modules.Echo.{to}"], offenders);
        }

        [Theory]
        [InlineData("Aspire", "Homeport.AppHost")]
        [InlineData("Tools", "Homeport.Seed")]
        [InlineData("Tests", "Homeport.Gateway.Tests.Unit")]
        public void Reports_A_Reference_To_A_Project_Outside_Services_Shared_And_Sdk(string directory, string name)
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Gateway/Homeport.Gateway", name),
                Fixture.Project($"{directory}/{name}"),
            ];

            var offenders = ReferenceRules.ReferencesInsideAUnit(projects);

            Assert.Equal([$"Homeport.Gateway -> {name}"], offenders);
        }

        [Fact]
        public void Accepts_A_Host_Referencing_Its_Layers_Shared_And_Sdk()
        {
            ProjectFile[] projects =
            [
                Fixture.Project(
                    "Modules/Echo/Homeport.Modules.Echo.Api",
                    "Homeport.Modules.Echo.Domain",
                    "Homeport.Modules.Echo.Application",
                    "Homeport.Modules.Echo.Infrastructure",
                    "Homeport.ServiceDefaults",
                    "Homeport.Module.Sdk"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Application"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure"),
                Fixture.Project("Shared/Homeport.ServiceDefaults"),
                Fixture.Project("Sdk/Homeport.Module.Sdk"),
            ];

            var offenders = ReferenceRules.ReferencesInsideAUnit(projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Accepts_An_Application_Referencing_Its_Domain_And_The_Contracts()
        {
            ProjectFile[] projects =
            [
                Fixture.Project(
                    "Platform/Metering/Homeport.Metering.Application",
                    "Homeport.Metering.Domain",
                    "Homeport.Contracts"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Domain"),
                Fixture.Project("Shared/Homeport.Contracts"),
            ];

            var offenders = ReferenceRules.ReferencesInsideAUnit(projects);

            Assert.Empty(offenders);
        }
    }

    public sealed class ReferencesBetweenUnits
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.ReferencesBetweenUnits(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Service_Referencing_Another_Service()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Api", "Homeport.Registry.Application"),
                Fixture.Project("Platform/Registry/Homeport.Registry.Application"),
            ];

            var offenders = ReferenceRules.ReferencesBetweenUnits(projects);

            Assert.Equal(["Homeport.Gateway.Api -> Homeport.Registry.Application"], offenders);
        }

        [Fact]
        public void Reports_A_Module_Referencing_A_Service()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure", "Homeport.ControlPlane"),
                Fixture.Project("Platform/ControlPlane/Homeport.ControlPlane"),
            ];

            var offenders = ReferenceRules.ReferencesBetweenUnits(projects);

            Assert.Equal(["Homeport.Modules.Echo.Infrastructure -> Homeport.ControlPlane"], offenders);
        }

        [Fact]
        public void Reports_A_Module_Referencing_Another_Module()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Application", "Homeport.Modules.Documents.Domain"),
                Fixture.Project("Modules/Documents/Homeport.Modules.Documents.Domain"),
            ];

            var offenders = ReferenceRules.ReferencesBetweenUnits(projects);

            Assert.Equal(["Homeport.Modules.Echo.Application -> Homeport.Modules.Documents.Domain"], offenders);
        }
    }

    public sealed class SharedReferences
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.SharedReferences(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_Shared_Referencing_A_Service()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Shared/Homeport.Contracts", "Homeport.Metering.Domain"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Domain"),
            ];

            var offenders = ReferenceRules.SharedReferences(projects);

            Assert.Equal(["Homeport.Contracts -> Homeport.Metering.Domain"], offenders);
        }

        [Fact]
        public void Reports_The_Sdk_Referencing_A_Module()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Sdk/Homeport.Module.Sdk", "Homeport.Modules.Echo.Api"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Api"),
            ];

            var offenders = ReferenceRules.SharedReferences(projects);

            Assert.Equal(["Homeport.Module.Sdk -> Homeport.Modules.Echo.Api"], offenders);
        }

        [Fact]
        public void Accepts_The_Sdk_Referencing_Shared()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Sdk/Homeport.Module.Sdk", "Homeport.Contracts"),
                Fixture.Project("Shared/Homeport.Contracts"),
            ];

            var offenders = ReferenceRules.SharedReferences(projects);

            Assert.Empty(offenders);
        }
    }

    public sealed class AspireReferences
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.AspireReferences(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Theory]
        [InlineData("Platform/Gateway", "Homeport.Gateway.Domain")]
        [InlineData("Platform/Gateway", "Homeport.Gateway.Application")]
        [InlineData("Modules/Echo", "Homeport.Modules.Echo.Infrastructure")]
        [InlineData("Sdk", "Homeport.Module.Sdk")]
        public void Reports_A_Reference_To_Anything_But_A_Host_Or_Shared(string directory, string name)
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Aspire/Homeport.AppHost", name),
                Fixture.Project($"{directory}/{name}"),
            ];

            var offenders = ReferenceRules.AspireReferences(projects);

            Assert.Equal([$"Homeport.AppHost -> {name}"], offenders);
        }

        [Fact]
        public void Accepts_Hosts_Of_Several_Services_And_Shared()
        {
            ProjectFile[] projects =
            [
                Fixture.Project(
                    "Aspire/Homeport.AppHost",
                    "Homeport.Gateway.Api",
                    "Homeport.ControlPlane",
                    "Homeport.Metering.Worker",
                    "Homeport.ServiceDefaults"),
                Fixture.Project("Platform/Gateway/Homeport.Gateway.Api"),
                Fixture.Project("Platform/ControlPlane/Homeport.ControlPlane"),
                Fixture.Project("Platform/Metering/Homeport.Metering.Worker"),
                Fixture.Project("Shared/Homeport.ServiceDefaults"),
            ];

            var offenders = ReferenceRules.AspireReferences(projects);

            Assert.Empty(offenders);
        }
    }

    public sealed class ReferencesOutsideSource
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.ReferencesOutsideSource(SolutionTree.Projects);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Reference_To_A_Project_Not_Under_Src()
        {
            ProjectFile[] projects = [Fixture.Project("Platform/Gateway/Homeport.Gateway", "Acme.Elsewhere")];

            var offenders = ReferenceRules.ReferencesOutsideSource(projects);

            Assert.Equal(["Homeport.Gateway -> Acme.Elsewhere"], offenders);
        }

        [Fact]
        public void Accepts_Any_Reference_From_A_Test()
        {
            ProjectFile[] projects = [Fixture.Project("Tests/Homeport.Gateway.Tests.Integration", "Acme.Elsewhere")];

            var offenders = ReferenceRules.ReferencesOutsideSource(projects);

            Assert.Empty(offenders);
        }
    }
}
