// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// The operator calling a platform service's admin API, as a caller token
/// tells the service (ADR-0020). It names no tenant and no client.
/// </summary>
/// <param name="Audience">The called platform service's audience.</param>
/// <param name="Subject">The subject of the operator's token.</param>
/// <param name="Name">The operator's display name, as it is at the time of the request.</param>
public sealed record OperatorCaller(string Audience, string Subject, string Name);
