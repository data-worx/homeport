// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>Reads the scopes a client's token grants.</summary>
internal static class ClientScopes
{
    private const char Separator = ' ';

    /// <summary>
    /// The scopes in <paramref name="claimType"/>, whether a provider sends
    /// them space-delimited in one claim or as one claim per scope.
    /// </summary>
    internal static IReadOnlyCollection<string> Of(ClaimsPrincipal user, string claimType)
    {
        return user.FindAll(claimType)
            .SelectMany(claim => claim.Value.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
