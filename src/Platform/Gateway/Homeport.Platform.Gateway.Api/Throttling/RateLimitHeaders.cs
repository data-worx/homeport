// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Platform.Gateway.Domain.Throttling;

namespace Homeport.Platform.Gateway.Api.Throttling;

/// <summary>
/// The RateLimit header fields of the IETF HTTPAPI draft
/// (draft-ietf-httpapi-ratelimit-headers): the policy of the tenant's bucket,
/// and what is left of it.
/// </summary>
internal static class RateLimitHeaders
{
    /// <summary>The header that states the policy: its quota and window.</summary>
    internal const string Policy = "RateLimit-Policy";

    /// <summary>The header that states what is left: remaining requests and seconds until reset.</summary>
    internal const string Limit = "RateLimit";

    /// <summary>The name of the one policy the gateway applies: the tenant's plan.</summary>
    internal const string PolicyName = "tenant";

    /// <summary>Sets both headers on <paramref name="headers"/>, replacing any a module sent.</summary>
    internal static void Set(IHeaderDictionary headers, ThrottleDecision decision)
    {
        headers[Policy] = string.Create(CultureInfo.InvariantCulture, $"\"{PolicyName}\";q={decision.Quota};w={decision.WindowSeconds}");
        headers[Limit] = string.Create(CultureInfo.InvariantCulture, $"\"{PolicyName}\";r={decision.Remaining};t={decision.ResetSeconds}");
    }
}
