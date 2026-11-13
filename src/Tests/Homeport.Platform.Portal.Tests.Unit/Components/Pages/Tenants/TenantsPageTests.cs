// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components.Pages.Tenants;
using Homeport.Shared.Client.Platform.ControlPlane.V1;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Tenants;

public sealed class TenantsPageTests : PortalTestContext
{
    private static readonly TenantResponse Finance = new(Guid.Parse("0193a5f0-0000-7000-8000-000000000001"), "Finance", 4);

    private static readonly TenantResponse Sales = new(Guid.Parse("0193a5f0-0000-7000-8000-000000000002"), "Sales", 1);

    [Fact]
    public void Lists_The_Tenants_With_Their_Clients()
    {
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Finance, Sales]));

        var (page, _) = RenderWithProviders<TenantsPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["Finance 4", "Sales 1"],
            page.FindAll("[data-tenant]").Select(row => $"{row.QuerySelector("a")!.TextContent.Trim()} {row.QuerySelector(".hp-num")!.TextContent.Trim()}")));
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_A_Problem_Loading_The_Tenants()
    {
        Api.Tenants = AdminResult<TenantsResponse>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<TenantsPage>();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (page.Find("[data-problem-title]").TextContent, page.Find("[data-problem-detail]").TextContent, page.Find("[data-trace-id]").TextContent)));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Tenants()
    {
        var (page, _) = RenderWithProviders<TenantsPage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty]")));
    }

    [Fact]
    public void Puts_The_Message_Of_A_Validation_Problem_At_The_Name_Field()
    {
        const string Message = "A tenant named 'Finance' already exists.";
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Finance]));
        Api.Change = AdminResult<TenantResponse>.Failure(StandInAdminApi.ValidationOf(TenantsApi.NameField, Message));
        var (page, dialogs) = RenderWithProviders<TenantsPage>();

        AddTenant(page, dialogs, "Finance");

        dialogs.WaitForAssertion(() =>
        {
            Assert.Equal(Message, dialogs.Find(".mud-input-helper-text.mud-input-error").TextContent.Trim());
            Assert.Empty(dialogs.FindAll("[data-problem]"));
        });
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_Another_Problem_In_The_Dialog()
    {
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Finance]));
        Api.Change = AdminResult<TenantResponse>.Failure(StandInAdminApi.Unavailable);
        var (page, dialogs) = RenderWithProviders<TenantsPage>();

        AddTenant(page, dialogs, "Marketing");

        dialogs.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (dialogs.Find("[data-problem-title]").TextContent, dialogs.Find("[data-problem-detail]").TextContent, dialogs.Find("[data-trace-id]").TextContent)));
    }

    [Fact]
    public void Lists_And_Selects_A_Created_Tenant()
    {
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Finance]));
        var (page, dialogs) = RenderWithProviders<TenantsPage>();

        AddTenant(page, dialogs, "Marketing");

        page.WaitForAssertion(() =>
        {
            Assert.Equal(2, page.FindAll("[data-tenant]").Count);
            Assert.Equal("Marketing", page.Find("[data-tenant-detail] h2").TextContent);
        });
    }

    private static void AddTenant(IRenderedComponent<TenantsPage> page, IRenderedComponent<MudBlazor.MudDialogProvider> dialogs, string name)
    {
        page.WaitForElement("[data-action=create]").Click();
        dialogs.WaitForElement($"input[data-field={TenantsApi.NameField}], [data-field={TenantsApi.NameField}] input").Input(name);
        dialogs.Find("[data-action=save]").Click();
    }
}
