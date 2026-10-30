// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Homeport.Shared.Authentication;

/// <summary>Logs every gateway token a service rejects.</summary>
internal sealed class GatewayTokenEvents(ILogger<GatewayTokenEvents> logger) : JwtBearerEvents
{
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        // The exception's type is the reason. Its message is left out, so
        // nothing of the token can reach the log.
        logger.LogWarning("A gateway token was rejected: {Reason}", context.Exception.GetType().Name);

        return base.AuthenticationFailed(context);
    }
}
