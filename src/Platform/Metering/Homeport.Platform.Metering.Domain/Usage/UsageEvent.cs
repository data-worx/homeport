// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>One request through the gateway, as it is counted: once, by its <see cref="EventId"/>.</summary>
/// <param name="EventId">Unique per request; the idempotency key.</param>
/// <param name="OccurredAt">When the gateway answered the request.</param>
/// <param name="TenantId">The tenant the request was made for.</param>
/// <param name="ClientId">The client that made it.</param>
/// <param name="ModuleId">The module it went to.</param>
/// <param name="Route">The route template from the manifest, never the concrete path.</param>
/// <param name="Meter">The meter it is counted in.</param>
/// <param name="Quantity">How much it counts.</param>
/// <param name="StatusCode">The status the client got.</param>
/// <param name="DurationMs">How long the gateway took to answer, in milliseconds.</param>
/// <param name="TraceId">The W3C trace id of the request.</param>
public sealed record UsageEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    string ClientId,
    string ModuleId,
    string Route,
    string Meter,
    int Quantity,
    int StatusCode,
    long DurationMs,
    string TraceId);
