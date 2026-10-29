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
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain", "Homeport.Shared.Authentication"),
                Fixture.Project("Shared/Homeport.Shared.Authentication"),
            ];

            var offenders = ReferenceRules.DomainReferences(projects, []);

            Assert.Equal(["Homeport.Modules.Echo.Domain -> Homeport.Shared.Authentication"], offenders);
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

    public sealed class ContractsReferences
    {
        [Fact]
        public void Finds_None_In_This_Repository()
        {
            var offenders = ReferenceRules.ContractsReferences(SolutionTree.Projects, DomainExceptions.Contracts);

            Assert.Empty(offenders);
        }

        [Fact]
        public void Reports_A_Project_Reference()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Shared/Homeport.Shared.Contracts", "Homeport.Shared.ServiceDefaults"),
                Fixture.Project("Shared/Homeport.Shared.ServiceDefaults"),
            ];

            var offenders = ReferenceRules.ContractsReferences(projects, DomainExceptions.Contracts);

            Assert.Equal(["Homeport.Shared.Contracts -> Homeport.Shared.ServiceDefaults"], offenders);
        }

        [Fact]
        public void Reports_A_Package_Reference()
        {
            ProjectFile[] projects = [Fixture.WithPackages("Shared/Homeport.Shared.Contracts", "Microsoft.EntityFrameworkCore")];

            var offenders = ReferenceRules.ContractsReferences(projects, DomainExceptions.Contracts);

            Assert.Equal(["Homeport.Shared.Contracts -> Microsoft.EntityFrameworkCore"], offenders);
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
        [InlineData("Aspire", "Homeport.Aspire.AppHost")]
        [InlineData("Tools", "Homeport.Tools.Seed")]
        [InlineData("Tests", "Homeport.Platform.Gateway.Tests.Unit")]
        public void Reports_A_Reference_To_A_Project_Outside_Services_Shared_And_Sdk(string directory, string name)
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Platform/Gateway/Homeport.Platform.Gateway", name),
                Fixture.Project($"{directory}/{name}"),
            ];

            var offenders = ReferenceRules.ReferencesInsideAUnit(projects);

            Assert.Equal([$"Homeport.Platform.Gateway -> {name}"], offenders);
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
                    "Homeport.Shared.ServiceDefaults",
                    "Homeport.Sdk"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Domain"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Application"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure"),
                Fixture.Project("Shared/Homeport.Shared.ServiceDefaults"),
                Fixture.Project("Sdk/Homeport.Sdk"),
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
                    "Platform/Metering/Homeport.Platform.Metering.Application",
                    "Homeport.Platform.Metering.Domain",
                    "Homeport.Shared.Contracts"),
                Fixture.Project("Platform/Metering/Homeport.Platform.Metering.Domain"),
                Fixture.Project("Shared/Homeport.Shared.Contracts"),
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
                Fixture.Project("Platform/Gateway/Homeport.Platform.Gateway.Api", "Homeport.Platform.Registry.Application"),
                Fixture.Project("Platform/Registry/Homeport.Platform.Registry.Application"),
            ];

            var offenders = ReferenceRules.ReferencesBetweenUnits(projects);

            Assert.Equal(["Homeport.Platform.Gateway.Api -> Homeport.Platform.Registry.Application"], offenders);
        }

        [Fact]
        public void Reports_A_Module_Referencing_A_Service()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Infrastructure", "Homeport.Platform.ControlPlane"),
                Fixture.Project("Platform/ControlPlane/Homeport.Platform.ControlPlane"),
            ];

            var offenders = ReferenceRules.ReferencesBetweenUnits(projects);

            Assert.Equal(["Homeport.Modules.Echo.Infrastructure -> Homeport.Platform.ControlPlane"], offenders);
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
                Fixture.Project("Shared/Homeport.Shared.Authentication", "Homeport.Platform.Metering.Domain"),
                Fixture.Project("Platform/Metering/Homeport.Platform.Metering.Domain"),
            ];

            var offenders = ReferenceRules.SharedReferences(projects);

            Assert.Equal(["Homeport.Shared.Authentication -> Homeport.Platform.Metering.Domain"], offenders);
        }

        [Fact]
        public void Reports_The_Sdk_Referencing_A_Module()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Sdk/Homeport.Sdk", "Homeport.Modules.Echo.Api"),
                Fixture.Project("Modules/Echo/Homeport.Modules.Echo.Api"),
            ];

            var offenders = ReferenceRules.SharedReferences(projects);

            Assert.Equal(["Homeport.Sdk -> Homeport.Modules.Echo.Api"], offenders);
        }

        [Fact]
        public void Accepts_The_Sdk_Referencing_Shared()
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Sdk/Homeport.Sdk", "Homeport.Shared.ServiceDefaults"),
                Fixture.Project("Shared/Homeport.Shared.ServiceDefaults"),
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
        [InlineData("Platform/Gateway", "Homeport.Platform.Gateway.Domain")]
        [InlineData("Platform/Gateway", "Homeport.Platform.Gateway.Application")]
        [InlineData("Modules/Echo", "Homeport.Modules.Echo.Infrastructure")]
        [InlineData("Sdk", "Homeport.Sdk")]
        public void Reports_A_Reference_To_Anything_But_A_Host_Or_Shared(string directory, string name)
        {
            ProjectFile[] projects =
            [
                Fixture.Project("Aspire/Homeport.Aspire.AppHost", name),
                Fixture.Project($"{directory}/{name}"),
            ];

            var offenders = ReferenceRules.AspireReferences(projects);

            Assert.Equal([$"Homeport.Aspire.AppHost -> {name}"], offenders);
        }

        [Fact]
        public void Accepts_Hosts_Of_Several_Services_And_Shared()
        {
            ProjectFile[] projects =
            [
                Fixture.Project(
                    "Aspire/Homeport.Aspire.AppHost",
                    "Homeport.Platform.Gateway.Api",
                    "Homeport.Platform.ControlPlane",
                    "Homeport.Platform.Metering.Worker",
                    "Homeport.Shared.ServiceDefaults"),
                Fixture.Project("Platform/Gateway/Homeport.Platform.Gateway.Api"),
                Fixture.Project("Platform/ControlPlane/Homeport.Platform.ControlPlane"),
                Fixture.Project("Platform/Metering/Homeport.Platform.Metering.Worker"),
                Fixture.Project("Shared/Homeport.Shared.ServiceDefaults"),
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
            ProjectFile[] projects = [Fixture.Project("Platform/Gateway/Homeport.Platform.Gateway", "Acme.Elsewhere")];

            var offenders = ReferenceRules.ReferencesOutsideSource(projects);

            Assert.Equal(["Homeport.Platform.Gateway -> Acme.Elsewhere"], offenders);
        }

        [Fact]
        public void Accepts_Any_Reference_From_A_Test()
        {
            ProjectFile[] projects = [Fixture.Project("Tests/Homeport.Platform.Gateway.Tests.Integration", "Acme.Elsewhere")];

            var offenders = ReferenceRules.ReferencesOutsideSource(projects);

            Assert.Empty(offenders);
        }
    }
}
