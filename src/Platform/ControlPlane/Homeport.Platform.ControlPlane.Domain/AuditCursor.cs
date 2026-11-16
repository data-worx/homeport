// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>
/// Where a page of the audit log ends: its last entry's moment and id. The
/// next page holds the entries that sort after it, newest first, so a page
/// stays stable while new entries arrive.
/// </summary>
/// <param name="OccurredAt">When the last entry of the page occurred.</param>
/// <param name="Id">The last entry's id, which orders entries of the same moment.</param>
public sealed record AuditCursor(DateTimeOffset OccurredAt, Guid Id)
{
    private const char Separator = '.';

    /// <summary>The cursor after <paramref name="entry"/>.</summary>
    public static AuditCursor After(AuditEntry entry)
    {
        return new AuditCursor(entry.OccurredAt, entry.Id);
    }

    /// <summary>Reads a cursor that <see cref="ToString"/> wrote.</summary>
    /// <param name="text">The cursor as a page handed it out.</param>
    /// <param name="cursor">The cursor, when it could be read.</param>
    public static bool TryParse(string text, [NotNullWhen(true)] out AuditCursor? cursor)
    {
        cursor = null;
        var parts = text.Split(Separator);
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParseExact(parts[1], "N", out var id))
        {
            return false;
        }

        cursor = new AuditCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }

    /// <summary>The cursor as a page hands it out: the moment's UTC ticks and the id.</summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{OccurredAt.UtcTicks}{Separator}{Id:N}");
    }
}
