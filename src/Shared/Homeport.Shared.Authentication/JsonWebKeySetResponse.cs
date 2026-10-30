// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Authentication;

/// <summary>The gateway's JWKS: every key a token it issued may be signed with.</summary>
/// <param name="Keys">The current key first, then the previous one while it is configured.</param>
public sealed record JsonWebKeySetResponse(
    [property: JsonPropertyName("keys")] IReadOnlyList<PublicJsonWebKey> Keys);
