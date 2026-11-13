// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components.Pages.Plans;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.Tests.Unit.Components.Pages.Plans;

public sealed class PlansPageTests : PortalTestContext
{
    private static readonly PlanResponse Gold = new(Guid.Parse("0193a5f0-0000-7000-8000-0000000000b1"), "Gold", 50, 100, 3);

    private static readonly PlanResponse Silver = new(Guid.Parse("0193a5f0-0000-7000-8000-0000000000b2"), "Silver", 10, 5, 0);

    private static readonly ProblemResponse PlanInUse = new(
        new Uri(ProblemConventions.TypePrefix + "plan-in-use"),
        "The plan is in use.",
        409,
        "The plan 'Gold' is used by at least one client. Move its clients to another plan first.",
        Instance: null,
        "5c3e6f0a8b1d4e2f9a7c6b5d4e3f2a1b",
        Errors: null);

    [Fact]
    public void Lists_The_Plans_With_Rate_Burst_And_Clients()
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold, Silver]));

        var (page, _) = RenderWithProviders<PlansPage>();

        page.WaitForAssertion(() => Assert.Equal(
            ["Gold 50 100 3", "Silver 10 5 0"],
            page.FindAll("[data-plan]").Select(row =>
                $"{row.QuerySelector("a")!.TextContent.Trim()} {row.QuerySelector("[data-rate]")!.TextContent.Trim()} "
                + $"{row.QuerySelector("[data-burst]")!.TextContent.Trim()} {row.QuerySelector("[data-clients]")!.TextContent.Trim()}")));
    }

    [Fact]
    public void Shows_The_Title_Detail_And_Trace_Id_Of_A_Problem_Loading_The_Plans()
    {
        Api.Plans = AdminResult<PlansResponse>.Failure(StandInAdminApi.Unavailable);

        var (page, _) = RenderWithProviders<PlansPage>();

        page.WaitForAssertion(() => Assert.Equal(
            (StandInAdminApi.Unavailable.Title, StandInAdminApi.Unavailable.Detail, StandInAdminApi.Unavailable.TraceId),
            (page.Find("[data-problem-title]").TextContent, page.Find("[data-problem-detail]").TextContent, page.Find("[data-trace-id]").TextContent)));
    }

    [Fact]
    public void Shows_The_Empty_State_Without_Plans()
    {
        var (page, _) = RenderWithProviders<PlansPage>();

        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-list-state=empty]")));
    }

    [Theory]
    [InlineData(PlansApi.NameField, "A plan named 'Gold' already exists.")]
    [InlineData(PlansApi.RequestsPerSecondField, "The requests per second must be at least 1.")]
    [InlineData(PlansApi.BurstField, "The burst must be at least 1.")]
    public void Puts_The_Message_Of_A_Validation_Problem_At_Its_Field(string field, string message)
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold]));
        Api.PlanChange = AdminResult<PlanResponse>.Failure(StandInAdminApi.ValidationOf(field, message));
        var (page, dialogs) = RenderWithProviders<PlansPage>();

        page.WaitForElement("[data-action=create]").Click();
        dialogs.WaitForElement("[data-action=save]").Click();

        dialogs.WaitForAssertion(() =>
        {
            Assert.Equal(message, dialogs.Find($"[data-field={field}] .mud-input-helper-text.mud-input-error, .mud-input-control:has([data-field={field}]) .mud-input-helper-text.mud-input-error").TextContent.Trim());
            Assert.Empty(dialogs.FindAll("[data-problem]"));
        });
    }

    [Fact]
    public void Lists_And_Selects_A_Created_Plan()
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold]));
        var (page, dialogs) = RenderWithProviders<PlansPage>();

        page.WaitForElement("[data-action=create]").Click();
        dialogs.WaitForElement($"[data-field={PlansApi.NameField}] input, input[data-field={PlansApi.NameField}]").Input("Bronze");
        dialogs.Find("[data-action=save]").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Equal(2, page.FindAll("[data-plan]").Count);
            Assert.Equal("Bronze", page.Find("[data-plan-detail] h2").TextContent);
        });
    }

    [Fact]
    public void Shows_Plan_In_Use_After_A_Delete_And_Keeps_The_Plan()
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold]));
        Api.PlanDeletion = AdminResult<Guid>.Failure(PlanInUse);
        var (page, dialogs) = RenderWithProviders<PlansPage>();

        page.WaitForElement("[data-action=delete]").Click();
        dialogs.WaitForElement("[data-action=confirm-delete]").Click();

        dialogs.WaitForAssertion(() => Assert.Equal(
            (PlanInUse.Title, PlanInUse.Detail, PlanInUse.TraceId),
            (dialogs.Find("[data-problem-title]").TextContent, dialogs.Find("[data-problem-detail]").TextContent, dialogs.Find("[data-trace-id]").TextContent)));
        Assert.Single(page.FindAll("[data-plan]"));
    }

    [Fact]
    public void Removes_A_Deleted_Plan_From_The_List()
    {
        Api.Plans = AdminResult<PlansResponse>.Success(new PlansResponse([Gold, Silver]));
        var (page, dialogs) = RenderWithProviders<PlansPage>();

        page.WaitForElement("[data-action=delete]").Click();
        dialogs.WaitForElement("[data-action=confirm-delete]").Click();

        page.WaitForAssertion(() => Assert.Equal(
            [Silver.Id.ToString()],
            page.FindAll("[data-plan]").Select(row => row.GetAttribute("data-plan"))));
    }
}
