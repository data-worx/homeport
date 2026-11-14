// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>The registered clients of the installation, or of one tenant.</summary>
/// <param name="Clients">The clients, ordered by client id.</param>
public sealed record ClientsResponse(IReadOnlyList<ClientResponse> Clients);
