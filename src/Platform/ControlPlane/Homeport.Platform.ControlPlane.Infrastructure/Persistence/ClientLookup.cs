// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application;
using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>Finds a client in <see cref="ControlPlaneDbContext"/>, read only.</summary>
internal sealed class ClientLookup(ControlPlaneDbContext database) : IClientLookup
{
    public Task<Client?> FindAsync(string clientId, CancellationToken cancellationToken)
    {
        return database.Clients
            .AsNoTracking()
            .Include(c => c.Plan)
            .SingleOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }
}
