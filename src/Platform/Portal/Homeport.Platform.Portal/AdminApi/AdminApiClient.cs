// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Platform.Registry.V1;
using Homeport.Shared.Client.Problems;
using Polly.Timeout;

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

    /// <summary>The title of the problem the portal describes an admin API that did not answer in time with.</summary>
    internal const string TimedOutTitle = "The admin API did not answer in time.";

    // A problem the portal describes itself never leaves it, so it takes no
    // type of Homeport's: RFC 9457's default says the status is all there is.
    private static readonly Uri UntypedProblem = new("about:blank");

    /// <inheritdoc />
    public Task<AdminResult<TenantsResponse>> GetTenantsAsync(CancellationToken cancellationToken)
    {
        return SendAsync<TenantsResponse>(HttpMethod.Get, TenantsApi.Path, body: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<TenantResponse>(HttpMethod.Post, TenantsApi.Path, JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<TenantResponse>> RenameTenantAsync(Guid tenantId, RenameTenantRequest request, CancellationToken cancellationToken)
    {
        var path = TenantsApi.TenantPath.Replace($"{{{TenantsApi.TenantIdRouteValue}}}", tenantId.ToString(), StringComparison.Ordinal);
        return SendAsync<TenantResponse>(HttpMethod.Put, path, JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<PlansResponse>> GetPlansAsync(CancellationToken cancellationToken)
    {
        return SendAsync<PlansResponse>(HttpMethod.Get, PlansApi.Path, body: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<PlanResponse>(HttpMethod.Post, PlansApi.Path, JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<PlanResponse>> ChangePlanAsync(Guid planId, ChangePlanRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<PlanResponse>(HttpMethod.Put, PlanPathOf(planId), JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The answer has no body; the deleted plan's id stands in for it.</remarks>
    public Task<AdminResult<Guid>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Delete, PlanPathOf(planId), body: null, accept: null, (_, _) => Task.FromResult(planId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<ClientsResponse>> GetClientsAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var path = tenantId is { } id ? $"{ClientsApi.Path}?{ClientsApi.TenantIdQuery}={id}" : ClientsApi.Path;
        return SendAsync<ClientsResponse>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<ClientResponse>> RegisterClientAsync(RegisterClientRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<ClientResponse>(HttpMethod.Post, ClientsApi.Path, JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<ClientResponse>> ChangeClientPlanAsync(string clientId, ChangeClientPlanRequest request, CancellationToken cancellationToken)
    {
        return SendAsync<ClientResponse>(HttpMethod.Put, ClientPathOf(clientId), JsonContent.Create(request), cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The answer has no body; the removed client's id stands in for it.</remarks>
    public Task<AdminResult<string>> RemoveClientAsync(string clientId, CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Delete, ClientPathOf(clientId), body: null, accept: null, (_, _) => Task.FromResult(clientId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<ModulesResponse>> GetModulesAsync(CancellationToken cancellationToken)
    {
        return SendAsync<ModulesResponse>(HttpMethod.Get, ModulesApi.Path, body: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminResult<AdminUsageResponse>> GetUsageAsync(UsageFilter filter, CancellationToken cancellationToken)
    {
        return SendAsync<AdminUsageResponse>(HttpMethod.Get, AdminUsageApi.Path + filter.QueryString(), body: null, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>The body is handed on as the admin API wrote it, so the download is the export itself.</remarks>
    public Task<AdminResult<byte[]>> ExportUsageAsync(UsageFilter filter, string mediaType, CancellationToken cancellationToken)
    {
        return SendAsync(
            HttpMethod.Get,
            AdminUsageApi.Path + filter.QueryString(),
            body: null,
            mediaType,
            (response, token) => response.Content.ReadAsByteArrayAsync(token),
            cancellationToken);
    }

    /// <summary>
    /// The path of a client. A client id is the identity provider's, so it may hold characters a
    /// path segment does not.
    /// </summary>
    private static string ClientPathOf(string clientId)
    {
        return ClientsApi.ClientPath.Replace($"{{{ClientsApi.ClientIdRouteValue}}}", Uri.EscapeDataString(clientId), StringComparison.Ordinal);
    }

    /// <summary>The path of a plan.</summary>
    private static string PlanPathOf(Guid planId)
    {
        return PlansApi.PlanPath.Replace($"{{{PlansApi.PlanIdRouteValue}}}", planId.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Sends the request and reads the value from the JSON body of a success.</summary>
    private Task<AdminResult<TValue>> SendAsync<TValue>(HttpMethod method, string path, HttpContent? body, CancellationToken cancellationToken)
    {
        return SendAsync(
            method,
            path,
            body,
            accept: null,
            async (response, token) => await response.Content.ReadFromJsonAsync<TValue>(token)
                ?? throw new InvalidOperationException($"The admin API answered {method} {path} without a body."),
            cancellationToken);
    }

    /// <summary>
    /// Sends the request with the operator's access token, and with <paramref name="accept"/>
    /// as the media type asked for where it is set; a missing or refused token asks for a
    /// new sign-in, and every error becomes a problem.
    /// </summary>
    private async Task<AdminResult<TValue>> SendAsync<TValue>(
        HttpMethod method,
        string path,
        HttpContent? body,
        string? accept,
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
        if (accept is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        }

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
        catch (Exception exception) when (exception is TimeoutRejectedException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // The resilience handler's timeout, or the client's own: the
            // operator's page shows a problem rather than losing its circuit.
            logger.LogWarning(exception, "The admin API at {BaseAddress} did not answer in time", http.BaseAddress);
            return AdminResult<TValue>.Failure(TimedOut());
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

    /// <summary>
    /// The problem of an error response. The gateway answers every error with
    /// Problem Details; a body that is not one is described by its status rather
    /// than trusted.
    /// </summary>
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

    /// <summary>The portal's own problem for an admin API it could not reach.</summary>
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

    /// <summary>The portal's own problem for an admin API that did not answer in time.</summary>
    private static ProblemResponse TimedOut()
    {
        return new ProblemResponse(
            UntypedProblem,
            TimedOutTitle,
            StatusCodes.Status504GatewayTimeout,
            "The gateway's admin endpoint did not answer in time. A change may have been made all the same; reload the page to see.",
            Instance: null,
            CurrentTraceId(),
            Errors: null);
    }

    /// <summary>The W3C trace id of the current activity, empty without one.</summary>
    private static string CurrentTraceId() => Activity.Current?.TraceId.ToHexString() ?? string.Empty;
}
