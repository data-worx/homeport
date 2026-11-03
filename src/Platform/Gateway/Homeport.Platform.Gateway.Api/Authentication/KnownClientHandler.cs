// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.ControlPlane;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>
/// Looks the client of an authenticated request up at the control plane. A
/// client it does not know fails the requirement, which the gateway answers
/// with <c>403</c>.
/// </summary>
/// <remarks>
/// A control plane that cannot be asked throws
/// <see cref="ControlPlaneUnavailableException"/>, which the gateway answers
/// with <c>503</c>.
/// </remarks>
internal sealed class KnownClientHandler(
    ControlPlaneClient controlPlane,
    IOptions<ClientAuthenticationOptions> options,
    ILogger<KnownClientHandler> logger)
    : AuthorizationHandler<KnownClientRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, KnownClientRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is
        // challenged; the control plane is not asked about it.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var claim = options.Value.ClientIdClaim;
        var clientId = context.User.FindFirst(claim)?.Value;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogWarning("A client token without the claim {ClientIdClaim} was refused", claim);
            context.Fail();
            return;
        }

        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        if (await controlPlane.FindAsync(clientId, cancellationToken) is null)
        {
            logger.LogWarning("Client {ClientId} is unknown to the control plane and was refused", clientId);
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}
