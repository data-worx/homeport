// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.Echo.V1;

/// <summary>Who called Echo, as the gateway's module token named the caller.</summary>
/// <param name="TenantId">The tenant the client acts for.</param>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="Subject">The subject of the client's token.</param>
/// <param name="Scopes">The scopes the client's token grants.</param>
public sealed record CallerResponse(Guid TenantId, string ClientId, string Subject, IReadOnlyList<string> Scopes);
