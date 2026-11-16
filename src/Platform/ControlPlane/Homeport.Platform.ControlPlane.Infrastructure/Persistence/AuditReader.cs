// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application;
using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>The audit entries in <see cref="ControlPlaneDbContext"/>, page by page.</summary>
internal sealed class AuditReader(ControlPlaneDbContext database) : IAuditReader
{
    /// <inheritdoc />
    public async Task<AuditPage> ReadAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        var entries = database.AuditEntries.AsNoTracking();
        if (query.From is { } from)
        {
            entries = entries.Where(e => e.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            entries = entries.Where(e => e.OccurredAt < to);
        }

        if (query.Actor is { } actor)
        {
            entries = entries.Where(e => e.OperatorSubject == actor);
        }

        if (query.Target is { } target)
        {
            entries = entries.Where(e => e.Target == target);
        }

        if (query.Before is { } before)
        {
            entries = entries.Where(e => e.OccurredAt < before.OccurredAt || (e.OccurredAt == before.OccurredAt && e.Id < before.Id));
        }

        // One more than the page holds tells whether another page follows.
        var page = await entries
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);

        return page.Count > query.Limit
            ? new AuditPage(page[..query.Limit], AuditCursor.After(page[query.Limit - 1]))
            : new AuditPage(page, null);
    }
}
