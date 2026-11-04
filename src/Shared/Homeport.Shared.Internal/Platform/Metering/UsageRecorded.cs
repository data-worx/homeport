// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Metering;

/// <summary>
/// One request through the gateway to a module, published once the gateway
/// answered it and counted by metering exactly once, by <see cref="EventId"/>.
/// </summary>
/// <remarks>
/// Changes are additive only; a breaking change is a new message type,
/// published next to this one for a release (ADR-0013).
/// </remarks>
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
public sealed record UsageRecorded(
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
