// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>
/// Set as a feature of a request whose client lacks the scope of the route,
/// so the gateway answers it as insufficient scope rather than unknown client.
/// </summary>
/// <param name="Scope">The scope the route declares.</param>
internal sealed record MissingScope(string Scope);
