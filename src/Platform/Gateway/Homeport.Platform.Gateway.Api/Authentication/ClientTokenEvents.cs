// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>Logs every client token the gateway rejects.</summary>
internal sealed class ClientTokenEvents(ILogger<ClientTokenEvents> logger) : JwtBearerEvents
{
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        // The exception's type is the reason. Its message is left out, so
        // nothing of the token can reach the log.
        logger.LogWarning("A client token was rejected: {Reason}", context.Exception.GetType().Name);

        return base.AuthenticationFailed(context);
    }
}
