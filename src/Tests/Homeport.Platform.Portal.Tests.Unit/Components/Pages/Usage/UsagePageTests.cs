// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components.Pages.Usage;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MudBlazor;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Usage;

public sealed class UsagePageTests : PortalTestContext
{
    private const string DownloadModule = "./Components/Pages/Usage/UsagePage.razor.js";

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTime November3 = new(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime November4 = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    private static readonly TenantResponse Sales = new(Guid.Parse("0193a5f0-0000-7000-8000-000000000001"), "Sales", 1);

    private static readonly TenantResponse Support = new(Guid.Parse("0193a5f0-0000-7000-8000-000000000002"), "Support", 1);

    private readonly BunitJSModuleInterop download;

    public UsagePageTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Sales, Support]));
        download = JSInterop.SetupModule(DownloadModule);
        download.SetupVoid("download", _ => true);
    }

    [Fact]
    public void Shows_A_Row_Per_Tenant_With_Its_Usage_Of_Each_Module_And_Meter_Summed_Over_The_Period()
    {
        UsageOf(
            new AdminUsageEntryResponse(November3, Support.Id, "echo", "requests", 1200),
            new AdminUsageEntryResponse(November4, Support.Id, "echo", "requests", 34),
            new AdminUsageEntryResponse(November3, Sales.Id, "ledger", "documents", 7),
            new AdminUsageEntryResponse(November4, Sales.Id, "echo", "requests", 5));

        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["Sales | 5 | 7", "Support | 1,234 | 0"],
            page.FindAll("[data-tenant]").Select(RowOf)));
    }

    [Fact]
    public void Shows_A_Column_Per_Module_And_Meter()
    {
        UsageOf(
            new AdminUsageEntryResponse(November3, Sales.Id, "ledger", "documents", 7),
            new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5));

        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["echo · requests", "ledger · documents"],
            page.FindAll("[data-column]").Select(column => column.GetAttribute("data-column"))));
    }

    [Fact]
    public void Shows_The_Total_Of_Each_Column()
    {
        UsageOf(
            new AdminUsageEntryResponse(November3, Support.Id, "echo", "requests", 1200),
            new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5),
            new AdminUsageEntryResponse(November3, Sales.Id, "ledger", "documents", 7));

        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            "Total | 1,205 | 7",
            RowOf(page.Find("[data-totals]"))));
    }

    [Fact]
    public void Names_A_Tenant_It_Does_Not_Know_By_Its_Id()
    {
        var deleted = Guid.Parse("0193a5f0-0000-7000-8000-0000000000ff");
        UsageOf(new AdminUsageEntryResponse(November3, deleted, "echo", "requests", 3));

        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            deleted.ToString(),
            page.Find($"[data-tenant='{deleted}'] [data-tenant-name]").TextContent));
    }

    [Fact]
    public void Reads_The_Current_Month_Up_To_Today_Per_Day_For_Every_Tenant()
    {
        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            new UsageFilter(new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), new(2026, 11, 16, 0, 0, 0, TimeSpan.Zero), UsageApi.Day, null, null, null),
            Assert.Single(Api.UsageReads)));
    }

    [Fact]
    public void Reads_Up_To_The_End_Of_The_Last_Day_Chosen()
    {
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-list-state=empty]");

        var to = page.FindComponents<MudDatePicker>().Single(picker => picker.Instance.Label == "To");
        page.InvokeAsync(() => to.Instance.DateChanged.InvokeAsync(new DateTime(2026, 11, 10)));

        page.WaitForAssertion(() => Assert.Equal(
            (new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero)),
            (Api.UsageReads[^1].From, Api.UsageReads[^1].To)));
    }

    [Fact]
    public void Reads_The_Usage_Of_The_Tenant_Chosen_In_The_Filter()
    {
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-list-state=empty]");

        var tenant = page.FindComponent<MudSelect<Guid>>();
        page.InvokeAsync(() => tenant.Instance.ValueChanged.InvokeAsync(Support.Id));

        page.WaitForAssertion(() => Assert.Equal(Support.Id, Api.UsageReads[^1].Tenant));
    }

    [Fact]
    public void Reads_The_Usage_Of_The_Module_And_Meter_Chosen_In_The_Filters()
    {
        UsageOf(new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5));
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-tenant]");

        var selects = page.FindComponents<MudSelect<string>>();
        page.InvokeAsync(() => selects.Single(select => select.Instance.Label == "Module").Instance.ValueChanged.InvokeAsync("echo"));
        page.InvokeAsync(() => selects.Single(select => select.Instance.Label == "Meter").Instance.ValueChanged.InvokeAsync("requests"));

        page.WaitForAssertion(() => Assert.Equal(("echo", "requests"), (Api.UsageReads[^1].Module, Api.UsageReads[^1].Meter)));
    }

    [Fact]
    public void Downloads_The_Csv_Of_The_Filters_Per_Hour_When_Hour_Is_Chosen()
    {
        UsageOf(new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5));
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-tenant]");

        var granularity = page.FindComponent<MudToggleGroup<string>>();
        page.InvokeAsync(() => granularity.Instance.ValueChanged.InvokeAsync(UsageApi.Hour));
        page.Find("[data-action=export-csv]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            ("homeport-usage-2026-11-01-2026-11-15-hour.csv", UsageApi.CsvMediaType, UsageApi.Hour),
            ((string)download.VerifyInvoke("download").Arguments[0]!, Api.Exports[^1].MediaType, Api.Exports[^1].Filter.Granularity)));
    }

    [Fact]
    public void Downloads_The_Json_Per_Day_By_Default()
    {
        UsageOf(new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5));
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-tenant]");

        page.Find("[data-action=export-json]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            ("homeport-usage-2026-11-01-2026-11-15-day.json", MediaTypeNames.Application.Json, UsageApi.Day),
            ((string)download.VerifyInvoke("download").Arguments[0]!, Api.Exports[^1].MediaType, Api.Exports[^1].Filter.Granularity)));
    }

    [Fact]
    public void Shows_The_Problem_Of_A_Download_And_Downloads_Nothing()
    {
        UsageOf(new AdminUsageEntryResponse(November3, Sales.Id, "echo", "requests", 5));
        Api.Export = AdminResult<byte[]>.Failure(StandInAdminApi.Unavailable);
        var (page, _) = RenderWithProviders<UsagePage>();
        page.WaitForElement("[data-tenant]");

        page.Find("[data-action=export-csv]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.TraceId, 0),
            (page.Find("[data-trace-id]").TextContent, download.Invocations.Count)));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Usage()
    {
        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty]")));
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_A_Problem_Loading_The_Usage()
    {
        Api.Usage = AdminResult<AdminUsageResponse>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<UsagePage>();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (page.Find("[data-problem-title]").TextContent, page.Find("[data-problem-detail]").TextContent, page.Find("[data-trace-id]").TextContent)));
    }

    private void UsageOf(params AdminUsageEntryResponse[] entries)
    {
        Api.Usage = AdminResult<AdminUsageResponse>.Success(new AdminUsageResponse(November3, November4.AddDays(1), UsageApi.Day, entries));
    }

    // The tenant's name, or the label of the totals, and the quantity of each column.
    private static string RowOf(AngleSharp.Dom.IElement row) => string.Join(
        " | ",
        [row.QuerySelector("[data-tenant-name]")?.TextContent.Trim() ?? row.QuerySelector("td")!.TextContent.Trim(),
         .. row.QuerySelectorAll("[data-quantity]").Select(cell => cell.TextContent.Trim())]);
}
