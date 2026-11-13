// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>Logs every operator token the gateway rejects on the admin port.</summary>
internal sealed class OperatorTokenEvents(ILogger<OperatorTokenEvents> logger) : JwtBearerEvents
{
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        // The exception's type is the reason. Its message is left out, so
        // nothing of the token can reach the log.
        logger.LogWarning("An operator token was rejected: {Reason}", context.Exception.GetType().Name);

        return base.AuthenticationFailed(context);
    }
}
