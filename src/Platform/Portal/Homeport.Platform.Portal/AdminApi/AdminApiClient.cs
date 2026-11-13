// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// Calls the admin API with the operator's access token from
/// <see cref="OperatorTokens"/> and reads every error as Problem Details
/// (ADR-0016). A failure to reach the API at all becomes a problem of the
/// portal's own, so a page shows every failure the same way.
/// </summary>
internal sealed class AdminApiClient(HttpClient http, OperatorTokens tokens, ILogger<AdminApiClient> logger) : IAdminApi
{
    /// <summary>The prefix the gateway serves the admin API below.</summary>
    internal const string PlatformPrefix = "/platform";

    // RFC 6750: the operator's access token is a bearer token.
    private const string BearerScheme = "Bearer";

    /// <summary>The title of the problem the portal describes an unreachable admin API with.</summary>
    internal const string UnreachableTitle = "The admin API is not reachable.";

    // A problem the portal describes itself never leaves it, so it takes no
    // type of Homeport's: RFC 9457's default says the status is all there is.
    private static readonly Uri UntypedProblem = new("about:blank");

    public Task<AdminResult<TenantsResponse>> GetTenantsAsync(CancellationToken cancellationToken)
    {
        return SendAsync<TenantsResponse>(HttpMethod.Get, TenantsApi.Path, body: null, cancellationToken);
    }

    public Task<AdminResult<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<TenantResponse>(HttpMethod.Post, TenantsApi.Path, JsonContent.Create(request), cancellationToken);
    }

    public Task<AdminResult<TenantResponse>> RenameTenantAsync(Guid tenantId, RenameTenantRequest request, CancellationToken cancellationToken)
    {
        var path = TenantsApi.TenantPath.Replace($"{{{TenantsApi.TenantIdRouteValue}}}", tenantId.ToString(), StringComparison.Ordinal);
        return SendAsync<TenantResponse>(HttpMethod.Put, path, JsonContent.Create(request), cancellationToken);
    }

    public Task<AdminResult<PlansResponse>> GetPlansAsync(CancellationToken cancellationToken)
    {
        return SendAsync<PlansResponse>(HttpMethod.Get, PlansApi.Path, body: null, cancellationToken);
    }

    public Task<AdminResult<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<PlanResponse>(HttpMethod.Post, PlansApi.Path, JsonContent.Create(request), cancellationToken);
    }

    public Task<AdminResult<PlanResponse>> ChangePlanAsync(Guid planId, ChangePlanRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<PlanResponse>(HttpMethod.Put, PlanPathOf(planId), JsonContent.Create(request), cancellationToken);
    }

    // The answer has no body; the deleted plan's id stands in for it.
    public Task<AdminResult<Guid>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Delete, PlanPathOf(planId), body: null, (_, _) => Task.FromResult(planId), cancellationToken);
    }

    private static string PlanPathOf(Guid planId)
    {
        return PlansApi.PlanPath.Replace($"{{{PlansApi.PlanIdRouteValue}}}", planId.ToString(), StringComparison.Ordinal);
    }

    private Task<AdminResult<TValue>> SendAsync<TValue>(HttpMethod method, string path, HttpContent? body, CancellationToken cancellationToken)
    {
        return SendAsync(
            method,
            path,
            body,
            async (response, token) => await response.Content.ReadFromJsonAsync<TValue>(token)
                ?? throw new InvalidOperationException($"The admin API answered {method} {path} without a body."),
            cancellationToken);
    }

    private async Task<AdminResult<TValue>> SendAsync<TValue>(
        HttpMethod method,
        string path,
        HttpContent? body,
        Func<HttpResponseMessage, CancellationToken, Task<TValue>> readValue,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokens.AccessTokenAsync(cancellationToken);
        if (accessToken is null)
        {
            return AdminResult<TValue>.SignInAgain();
        }

        using var request = new HttpRequestMessage(method, PlatformPrefix + path) { Content = body };
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, accessToken);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "The admin API at {BaseAddress} is not reachable", http.BaseAddress);
            return AdminResult<TValue>.Failure(Unreachable());
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogDebug("The admin API refused the operator's token; the operator signs in again");
                return AdminResult<TValue>.SignInAgain();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminResult<TValue>.Failure(await ProblemOfAsync(response, cancellationToken));
            }

            return AdminResult<TValue>.Success(await readValue(response, cancellationToken));
        }
    }

    // The gateway answers every error with Problem Details; a body that is
    // not one is described by its status rather than trusted.
    private async Task<ProblemResponse> ProblemOfAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.ProblemJson
            && await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken) is { } problem)
        {
            logger.LogDebug("The admin API answered {StatusCode} with {ProblemType}", (int)response.StatusCode, problem.Type);
            return problem;
        }

        logger.LogWarning("The admin API answered {StatusCode} without Problem Details", (int)response.StatusCode);
        return new ProblemResponse(
            UntypedProblem,
            response.ReasonPhrase ?? "The admin API answered with an error.",
            (int)response.StatusCode,
            Detail: null,
            Instance: null,
            CurrentTraceId(),
            Errors: null);
    }

    private static ProblemResponse Unreachable()
    {
        return new ProblemResponse(
            UntypedProblem,
            UnreachableTitle,
            StatusCodes.Status503ServiceUnavailable,
            "The portal could not connect to the gateway's admin endpoint. Nothing was changed.",
            Instance: null,
            CurrentTraceId(),
            Errors: null);
    }

    private static string CurrentTraceId() => Activity.Current?.TraceId.ToHexString() ?? string.Empty;
}
