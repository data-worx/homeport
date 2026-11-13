// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>The operator's tokens from the identity provider.</summary>
/// <param name="AccessToken">The token the admin API accepts.</param>
/// <param name="RefreshToken">The token that gets a new access token, if the identity provider issued one.</param>
/// <param name="ExpiresAt">When the access token expires.</param>
internal sealed record OperatorTokenSet(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);
