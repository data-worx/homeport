// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Who calls a module, as a module token tells the module, or a platform
/// service, as a caller token tells the service (ADR-0019).
/// </summary>
/// <param name="Audience">The called module's id or platform service's audience.</param>
/// <param name="TenantId">The tenant the client acts for.</param>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="Subject">The subject of the client's token.</param>
/// <param name="Scopes">The scopes the client's token grants.</param>
public sealed record ModuleCaller(
    string Audience,
    Guid TenantId,
    string ClientId,
    string Subject,
    IReadOnlyCollection<string> Scopes);
