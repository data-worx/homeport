// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>Who calls a module, as a module token tells the module.</summary>
/// <param name="ModuleId">The called module, the token's audience.</param>
/// <param name="TenantId">The tenant the client acts for.</param>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="Subject">The subject of the client's token.</param>
/// <param name="Scopes">The scopes the client's token grants.</param>
public sealed record ModuleCaller(
    string ModuleId,
    Guid TenantId,
    string ClientId,
    string Subject,
    IReadOnlyCollection<string> Scopes);
