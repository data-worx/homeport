// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application;

/// <summary>Finds a client with the plan its requests are held to.</summary>
public interface IClientLookup
{
    /// <summary>
    /// The client <paramref name="clientId"/> with its <see cref="Client.Plan"/>,
    /// or <see langword="null"/> when the control plane knows no such client.
    /// </summary>
    Task<Client?> FindAsync(string clientId, CancellationToken cancellationToken);
}
