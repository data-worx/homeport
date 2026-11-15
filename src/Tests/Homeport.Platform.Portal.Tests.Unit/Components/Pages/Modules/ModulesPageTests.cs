// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.ApiDocs;
using Homeport.Platform.Portal.Components.Pages.Modules;
using Homeport.Shared.Client.Platform.Registry.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Modules;

public sealed class ModulesPageTests : PortalTestContext
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly ModuleResponse Echo = new(
        "echo",
        [
            new ModuleInstanceResponse("echo-a", new Uri("http://echo-a:8080"), "1.0.0", 1, Now.AddSeconds(-9), Now.AddSeconds(21)),
            new ModuleInstanceResponse("echo-b", new Uri("http://echo-b:8080"), "1.1.0", 1, Now.AddSeconds(-4), Now.AddSeconds(26)),
        ],
        [
            new ModuleRouteResponse("POST", "/v1/echo", "echo.invoke", "requests", 1, "1.1.0"),
            new ModuleRouteResponse("GET", "/v1/whoami", "echo.invoke", "requests", 1, "1.0.0"),
        ]);

    private static readonly ModuleResponse Ledger = new(
        "ledger",
        [new ModuleInstanceResponse("ledger-1", new Uri("http://ledger:8080"), "2.4.1", 1, Now.AddSeconds(-2), Now.AddSeconds(28))],
        [new ModuleRouteResponse("GET", "/v1/entries", "ledger.read", "requests", 1, "2.4.1")]);

    public ModulesPageTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
    }

    [Fact]
    public void Lists_The_Modules_With_Versions_Contract_Instances_Heartbeat_And_Routes()
    {
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo, Ledger]));

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["echo | 1.0.0, 1.1.0 | v1 | 2 | 4 s | 2", "ledger | 2.4.1 | v1 | 1 | 2 s | 1"],
            page.FindAll("[data-module]").Select(row => string.Join(
                " | ",
                row.QuerySelector("a")!.TextContent.Trim(),
                row.QuerySelector("[data-version]")!.TextContent.Trim(),
                row.QuerySelector("[data-contract]")!.TextContent.Trim(),
                row.QuerySelector("[data-instances]")!.TextContent.Trim(),
                row.QuerySelector("[data-heartbeat]")!.TextContent.Trim(),
                row.QuerySelector("[data-routes]")!.TextContent.Trim()))));
    }

    [Fact]
    public void Shows_Every_Instance_Of_The_Selected_Module()
    {
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo, Ledger]));

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["echo-a 1.0.0 · v1 9 s", "echo-b 1.1.0 · v1 4 s"],
            page.FindAll("[data-instances-of=echo] [data-instance]").Select(row =>
                $"{row.GetAttribute("data-instance")} {row.QuerySelector("[data-version]")!.TextContent.Trim()} {row.QuerySelector("[data-heartbeat]")!.TextContent.Trim()}")));
    }

    [Fact]
    public void Shows_Each_Route_With_The_Version_Whose_Declaration_Applies()
    {
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo]));

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["POST /v1/echo 1.1.0", "GET /v1/whoami 1.0.0"],
            page.FindAll("[data-routes-of=echo] [data-route]").Select(row =>
                $"{row.GetAttribute("data-route")} {row.QuerySelector("[data-version]")!.TextContent.Trim()}")));
    }

    [Fact]
    public void Shows_Another_Module_When_It_Is_Selected()
    {
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo, Ledger]));
        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForElement("[data-module=ledger]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            ["ledger-1"],
            page.FindAll("[data-instances-of=ledger] [data-instance]").Select(row => row.GetAttribute("data-instance"))));
    }

    [Fact]
    public void Links_A_Module_To_Its_Document_Where_The_Docs_Are_Set()
    {
        Services.AddSingleton(Options.Create(new ApiDocsOptions { Url = new Uri("http://localhost:5100/platform/docs") }));
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo]));

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.Equal(
            "http://localhost:5100/platform/docs/echo",
            page.Find("[data-module=echo] [data-docs]").GetAttribute("href")));
    }

    [Fact]
    public void Links_No_Document_Without_The_Docs()
    {
        Api.Modules = AdminResult<ModulesResponse>.Success(new ModulesResponse([Echo]));

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForElement("[data-module=echo]");
        Assert.Empty(page.FindAll("[data-docs]"));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Modules()
    {
        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty]")));
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_A_Problem_Loading_The_Modules()
    {
        Api.Modules = AdminResult<ModulesResponse>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<ModulesPage>();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (page.Find("[data-problem-title]").TextContent, page.Find("[data-problem-detail]").TextContent, page.Find("[data-trace-id]").TextContent)));
    }
}
