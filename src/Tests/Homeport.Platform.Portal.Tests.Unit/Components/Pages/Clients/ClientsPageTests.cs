// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components.Pages.Clients;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using MudBlazor;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Clients;

public sealed class ClientsPageTests : PortalTestContext
{
    private static readonly TenantResponse Sales = new(Guid.Parse("0193a5f0-0000-7000-8000-0000000000a1"), "Sales", 1);

    private static readonly TenantResponse Support = new(Guid.Parse("0193a5f0-0000-7000-8000-0000000000a2"), "Support", 1);

    private static readonly PlanResponse Gold = new(Guid.Parse("0193a5f0-0000-7000-8000-0000000000b1"), "Gold", 50, 100, 2);

    private static readonly ClientResponse Billing = new("billing-service", Sales.Id, Sales.Name, Gold.Id, Gold.Name);

    private static readonly ClientResponse Helpdesk = new("helpdesk-bot", Support.Id, Support.Name, Gold.Id, Gold.Name);

    public ClientsPageTests()
    {
        Api.Tenants = AdminResult<TenantsResponse>.Success(new TenantsResponse([Sales, Support]));
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold]));
    }

    [Fact]
    public void Lists_The_Clients_With_Tenant_And_Plan()
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Billing, Helpdesk]));

        var (page, _) = RenderWithProviders<ClientsPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["billing-service Sales Gold", "helpdesk-bot Support Gold"],
            page.FindAll("[data-client]").Select(row =>
                $"{row.QuerySelector("[data-client-id]")!.TextContent.Trim()} {row.QuerySelector("[data-tenant]")!.TextContent.Trim()} "
                + $"{row.QuerySelector("[data-plan]")!.TextContent.Trim()}")));
    }

    [Fact]
    public void Lists_Only_The_Clients_Of_The_Tenant_Chosen_In_The_Filter()
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Billing, Helpdesk]));
        Api.Clients[Support.Id] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Helpdesk]));
        var (page, _) = RenderWithProviders<ClientsPage>();
        page.WaitForElement("[data-client]");

        var filter = page.FindComponent<MudSelect<Guid>>();
        page.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(Support.Id));

        page.WaitForAssertion(() => Assert.Equal(
            [Helpdesk.ClientId],
            page.FindAll("[data-client]").Select(row => row.GetAttribute("data-client"))));
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_A_Problem_Loading_The_Clients()
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<ClientsPage>();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (page.Find("[data-problem-title]").TextContent, page.Find("[data-problem-detail]").TextContent, page.Find("[data-trace-id]").TextContent)));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Clients()
    {
        var (page, _) = RenderWithProviders<ClientsPage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty] [data-action=register]")));
    }

    [Fact]
    public void Points_To_Tenants_And_Plans_Instead_Of_Registering_Without_A_Plan()
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([]));

        var (page, _) = RenderWithProviders<ClientsPage>();

        page.WaitForAssertion(() =>
        {
            Assert.NotNull(page.Find("[data-list-state=empty]"));
            Assert.Empty(page.FindAll("[data-action=register]"));
        });
    }

    [Theory]
    [InlineData(ClientsApi.ClientIdField, "The client 'billing-service' is already registered.")]
    [InlineData(ClientsApi.TenantIdField, "There is no such tenant.")]
    [InlineData(ClientsApi.PlanIdField, "There is no such plan.")]
    public void Puts_The_Message_Of_A_Validation_Problem_At_Its_Field(string field, string message)
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Billing]));
        Api.ClientChange = AdminResult<ClientResponse>.Failure(StandInAdminApi.ValidationOf(field, message));
        var (page, dialogs) = RenderWithProviders<ClientsPage>();

        page.WaitForElement("[data-action=register]").Click();
        dialogs.WaitForElement("[data-action=save]").Click();

        dialogs.WaitForAssertion(() =>
        {
            Assert.Equal(message, dialogs.Find($".mud-input-control:has([data-field={field}]) .mud-input-helper-text.mud-input-error").TextContent.Trim());
            Assert.Empty(dialogs.FindAll("[data-problem]"));
        });
    }

    [Fact]
    public void Lists_A_Registered_Client_In_Order()
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Helpdesk]));
        var (page, dialogs) = RenderWithProviders<ClientsPage>();

        page.WaitForElement("[data-action=register]").Click();
        dialogs.WaitForElement($"input[data-field={ClientsApi.ClientIdField}], [data-field={ClientsApi.ClientIdField}] input").Input("billing-service");
        dialogs.Find("[data-action=save]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            ["billing-service", "helpdesk-bot"],
            page.FindAll("[data-client]").Select(row => row.GetAttribute("data-client"))));
    }

    [Fact]
    public void Removes_A_Client_After_The_Confirmation()
    {
        Api.Clients[Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([Billing, Helpdesk]));
        var (page, dialogs) = RenderWithProviders<ClientsPage>();

        page.WaitForElement("[data-action=remove]").Click();
        dialogs.WaitForElement("[data-action=confirm-remove]").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Equal([Billing.ClientId], Api.RemovedClients);
            Assert.Equal([Helpdesk.ClientId], page.FindAll("[data-client]").Select(row => row.GetAttribute("data-client")));
        });
    }
}
