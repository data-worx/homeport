// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.AdminApi;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.Components.Pages.Audit;

/// <summary>A service that keeps an audit log (ADR-0020).</summary>
internal enum AuditService
{
    /// <summary>The control plane: changes to tenants, plans and clients.</summary>
    ControlPlane,

    /// <summary>Metering: reads of the tenants' usage.</summary>
    Metering,
}

/// <summary>One entry of a service's audit log, as the page shows it.</summary>
/// <param name="Service">The service whose audit log holds it.</param>
/// <param name="Id">The entry's id.</param>
/// <param name="OccurredAt">When it was done, in UTC.</param>
/// <param name="OperatorSubject">The operator's subject at the identity provider.</param>
/// <param name="OperatorName">The operator's display name at the time.</param>
/// <param name="Action">What was done.</param>
/// <param name="Target">What it was done to.</param>
/// <param name="Summary">What was done, in one English sentence.</param>
internal sealed record AuditRow(
    AuditService Service,
    Guid Id,
    DateTime OccurredAt,
    string OperatorSubject,
    string OperatorName,
    string Action,
    string Target,
    string Summary);

/// <summary>One page of a service's audit log.</summary>
/// <param name="Rows">The entries of the page, newest first.</param>
/// <param name="Next">Where the next page starts; <see langword="null"/> on the last page.</param>
internal sealed record AuditRows(IReadOnlyList<AuditRow> Rows, string? Next);

/// <summary>Reads the page of one service's audit log that starts after <paramref name="before"/>, or the first.</summary>
/// <param name="before">The <see cref="AuditRows.Next"/> of the page before, or <see langword="null"/>.</param>
internal delegate Task<AdminResult<AuditRows>> AuditPageReader(string? before);

/// <summary>
/// The audit logs of several services as one list, newest first. Each
/// service is read page by page, and only when the list needs its next entry:
/// an entry is shown only once every service that may still hold a newer one
/// has been asked. A service that answers with a problem is left out, and
/// its problem is kept as a gap of the list.
/// </summary>
internal sealed class AuditTrail
{
    private readonly IReadOnlyList<Source> sources;
    private readonly List<AuditRow> rows = [];

    /// <summary>Creates the list over the audit logs <paramref name="readers"/> read.</summary>
    internal AuditTrail(IReadOnlyDictionary<AuditService, AuditPageReader> readers)
    {
        sources = readers.Select(reader => new Source(reader.Key, reader.Value)).ToList();
    }

    /// <summary>The entries shown so far, newest first.</summary>
    internal IReadOnlyList<AuditRow> Rows => rows;

    /// <summary>The services left out, each with the problem it answered with.</summary>
    internal IReadOnlyList<(AuditService Service, ProblemResponse Problem)> Gaps =>
        sources.Where(source => source.Problem is not null).Select(source => (source.Service, source.Problem!)).ToList();

    /// <summary>Whether every service answered with a problem, so the list has nothing to show.</summary>
    internal bool Failed => sources.All(source => source.Problem is not null);

    /// <summary>Whether the operator has to sign in again before the list can be read.</summary>
    internal bool SignInRequired { get; private set; }

    /// <summary>Whether a service may hold entries the list does not show yet.</summary>
    internal bool HasMore => sources.Any(source => source.Buffer.Count > 0 || source.MayHoldMore);

    /// <summary>Shows up to <paramref name="count"/> more entries.</summary>
    internal async Task ShowMoreAsync(int count)
    {
        for (var shown = 0; shown < count; shown++)
        {
            // Every service that may hold a newer entry than the ones waiting
            // is asked before the newest of them is shown.
            await Task.WhenAll(sources.Where(source => source.Buffer.Count == 0 && source.MayHoldMore).Select(ReadAsync));
            if (SignInRequired)
            {
                return;
            }

            var newest = sources
                .Where(source => source.Buffer.Count > 0)
                .MaxBy(source => (source.Buffer.Peek().OccurredAt, source.Buffer.Peek().Id));
            if (newest is null)
            {
                return;
            }

            rows.Add(newest.Buffer.Dequeue());
        }
    }

    /// <summary>Reads the next page of <paramref name="source"/> into its buffer.</summary>
    private async Task ReadAsync(Source source)
    {
        var result = await source.Read(source.Next);
        if (result.SignInRequired)
        {
            SignInRequired = true;
        }
        else if (result.Problem is { } problem)
        {
            source.Problem = problem;
        }
        else
        {
            foreach (var row in result.Value!.Rows)
            {
                source.Buffer.Enqueue(row);
            }

            source.Next = result.Value.Next;
            source.Started = true;
        }
    }

    /// <summary>One service's audit log, read so far.</summary>
    /// <param name="service">The service.</param>
    /// <param name="read">Reads one page of its audit log.</param>
    private sealed class Source(AuditService service, AuditPageReader read)
    {
        /// <summary>The service.</summary>
        internal AuditService Service { get; } = service;

        /// <summary>Reads one page of its audit log.</summary>
        internal AuditPageReader Read { get; } = read;

        /// <summary>The entries read but not shown yet, newest first.</summary>
        internal Queue<AuditRow> Buffer { get; } = new();

        /// <summary>Where its next page starts.</summary>
        internal string? Next { get; set; }

        /// <summary>Whether its first page was read.</summary>
        internal bool Started { get; set; }

        /// <summary>The problem it answered with, after which it is not read again.</summary>
        internal ProblemResponse? Problem { get; set; }

        /// <summary>Whether a page not read yet may hold entries.</summary>
        internal bool MayHoldMore => Problem is null && (!Started || Next is not null);
    }
}
