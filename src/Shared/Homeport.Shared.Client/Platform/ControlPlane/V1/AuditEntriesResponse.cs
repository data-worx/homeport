// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>One page of the audit log, newest first.</summary>
/// <param name="Entries">The entries of the page.</param>
/// <param name="Next">
/// The value of <see cref="AuditApi.BeforeParameter"/> that reads the next,
/// older page; <see langword="null"/> on the last page.
/// </param>
public sealed record AuditEntriesResponse(IReadOnlyList<AuditEntryResponse> Entries, string? Next);
