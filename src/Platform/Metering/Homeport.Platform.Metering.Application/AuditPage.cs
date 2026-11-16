// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain;

namespace Homeport.Platform.Metering.Application;

/// <summary>One page of the audit log, newest first.</summary>
/// <param name="Entries">The entries of the page.</param>
/// <param name="Next">Where the next, older page starts; <see langword="null"/> on the last page.</param>
public sealed record AuditPage(IReadOnlyList<AuditEntry> Entries, AuditCursor? Next);
