// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components.Pages.Audit;
using MudBlazor;
using ControlPlaneAudit = Homeport.Shared.Client.Platform.ControlPlane.V1.AuditEntriesResponse;
using ControlPlaneEntry = Homeport.Shared.Client.Platform.ControlPlane.V1.AuditEntryResponse;
using MeteringAudit = Homeport.Shared.Client.Platform.Metering.V1.AuditEntriesResponse;
using MeteringEntry = Homeport.Shared.Client.Platform.Metering.V1.AuditEntryResponse;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Audit;

public sealed class AuditPageTests : PortalTestContext
{
    private static readonly DateTime Start = new(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Merges_The_Entries_Of_Both_Services_By_Time_Newest_First()
    {
        Api.ControlPlaneAudit = _ => ControlPlanePage([ControlPlaneAt(3), ControlPlaneAt(1)], next: null);
        Api.MeteringAudit = _ => MeteringPage([MeteringAt(2), MeteringAt(0)], next: null);

        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["09:03 control-plane", "09:02 metering", "09:01 control-plane", "09:00 metering"],
            RowsOf(page)));
    }

    [Fact]
    public void Shows_Time_Service_Operator_Action_Summary_And_Target()
    {
        Api.ControlPlaneAudit = _ => ControlPlanePage([ControlPlaneAt(3)], next: null);

        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.Equal(
            "2026-11-16 09:03:00 | Control plane | Dev Operator | operator-1 | tenant.renamed | Renamed the tenant. | tenants/one",
            string.Join(
                " | ",
                new[] { "[data-time]", "[data-service-name]", "[data-operator-name]", "[data-operator-subject]", "[data-action]", "[data-summary]", "[data-target]" }
                    .Select(cell => page.Find($"[data-entry] {cell}").TextContent.Trim()))));
    }

    [Fact]
    public void Reads_A_Page_Of_A_Service_Again_Before_Showing_An_Older_Entry_Of_The_Other()
    {
        Api.ControlPlaneAudit = before => before is null
            ? ControlPlanePage([ControlPlaneAt(10), ControlPlaneAt(8)], next: "page-2")
            : ControlPlanePage([ControlPlaneAt(5)], next: null);
        Api.MeteringAudit = _ => MeteringPage([MeteringAt(9), MeteringAt(1)], next: null);

        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["09:10 control-plane", "09:09 metering", "09:08 control-plane", "09:05 control-plane", "09:01 metering"],
            RowsOf(page)));
    }

    [Fact]
    public void Reads_Both_Services_With_The_Operator_Chosen_In_The_Filter()
    {
        var (page, _) = RenderWithProviders<AuditPage>();
        page.WaitForElement("[data-list-state=empty]");

        var actor = page.FindComponents<MudTextField<string>>().Single(field => field.Instance.Label == "Operator subject");
        page.InvokeAsync(() => actor.Instance.ValueChanged.InvokeAsync(" operator-1 "));

        var expected = new AuditFilter(From: null, To: null, "operator-1", Target: null);
        page.WaitForAssertion(() => Assert.Equal(
            ((expected, (string?)null), (expected, (string?)null)),
            (Api.ControlPlaneAuditReads[^1], Api.MeteringAuditReads[^1])));
    }

    [Fact]
    public void Reads_Up_To_The_End_Of_The_Last_Day_Chosen()
    {
        var (page, _) = RenderWithProviders<AuditPage>();
        page.WaitForElement("[data-list-state=empty]");

        var to = page.FindComponents<MudDatePicker>().Single(picker => picker.Instance.Label == "To");
        page.InvokeAsync(() => to.Instance.DateChanged.InvokeAsync(new DateTime(2026, 11, 10)));

        page.WaitForAssertion(() => Assert.Equal(
            new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero),
            Api.ControlPlaneAuditReads[^1].Filter.To));
    }

    [Fact]
    public void Shows_The_Entries_Of_One_Service_And_Marks_The_Other_When_It_Answers_With_A_Problem()
    {
        Api.ControlPlaneAudit = _ => ControlPlanePage([ControlPlaneAt(3)], next: null);
        Api.MeteringAudit = _ => AdminResult<MeteringAudit>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ("09:03 control-plane", "Metering not reachable", StandInAdminApi.Unavailable.TraceId),
            (Assert.Single(RowsOf(page)), page.Find("[data-list-state=degraded] [data-degraded-title]").TextContent.Trim(), page.Find("[data-list-state=degraded] [data-trace-id]").TextContent)));
    }

    [Fact]
    public void Shows_The_Error_State_When_No_Service_Answers()
    {
        Api.ControlPlaneAudit = _ => AdminResult<ControlPlaneAudit>.Failure(StandInAdminApi.Unavailable);
        Api.MeteringAudit = _ => AdminResult<MeteringAudit>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.Equal(
            (1, 0),
            (page.FindAll("[data-list-state=error]").Count, page.FindAll("[data-list-state=degraded]").Count)));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Entries()
    {
        var (page, _) = RenderWithProviders<AuditPage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty]")));
    }

    [Fact]
    public void Shows_Fifty_Entries_And_The_Rest_On_Load_More()
    {
        Api.ControlPlaneAudit = _ => ControlPlanePage(Enumerable.Range(0, 51).Reverse().Select(ControlPlaneAt).ToList(), next: null);
        var (page, _) = RenderWithProviders<AuditPage>();
        page.WaitForAssertion(() => Assert.Equal(50, page.FindAll("[data-entry]").Count));

        page.Find("[data-action=more]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            (51, 0),
            (page.FindAll("[data-entry]").Count, page.FindAll("[data-action=more]").Count)));
    }

    private static List<string> RowsOf(IRenderedComponent<AuditPage> page)
    {
        return page.FindAll("[data-entry]")
            .Select(row => $"{row.QuerySelector("[data-time]")!.TextContent.Trim()[11..16]} {row.GetAttribute("data-service")}")
            .ToList();
    }

    private static ControlPlaneEntry ControlPlaneAt(int minute)
    {
        return new ControlPlaneEntry(Guid.NewGuid(), Start.AddMinutes(minute), "operator-1", "Dev Operator", "tenant.renamed", "tenants/one", "Renamed the tenant.");
    }

    private static MeteringEntry MeteringAt(int minute)
    {
        return new MeteringEntry(Guid.NewGuid(), Start.AddMinutes(minute), "operator-1", "Dev Operator", "usage.read", "tenants", "Read the usage.");
    }

    private static AdminResult<ControlPlaneAudit> ControlPlanePage(IReadOnlyList<ControlPlaneEntry> entries, string? next)
    {
        return AdminResult<ControlPlaneAudit>.Success(new ControlPlaneAudit(entries, next));
    }

    private static AdminResult<MeteringAudit> MeteringPage(IReadOnlyList<MeteringEntry> entries, string? next)
    {
        return AdminResult<MeteringAudit>.Success(new MeteringAudit(entries, next));
    }
}
