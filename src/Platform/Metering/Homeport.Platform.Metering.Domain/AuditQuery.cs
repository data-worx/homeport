// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.Metering.Domain;

/// <summary>
/// One page of the audit log the operator reads: the entries newest first,
/// optionally in a period, of one operator and of one target, older than
/// <see cref="Before"/>.
/// </summary>
public sealed record AuditQuery
{
    /// <summary>The period ends before it starts, or is empty.</summary>
    public const string EmptyPeriodCode = "audit.period";

    /// <summary>The actor is longer than any subject an entry holds.</summary>
    public const string ActorTooLongCode = "audit.actor";

    /// <summary>The target is longer than any target an entry holds.</summary>
    public const string TargetTooLongCode = "audit.target";

    /// <summary>The cursor is not one a page handed out.</summary>
    public const string InvalidCursorCode = "audit.before";

    /// <summary>The limit is not between one and <see cref="MaxLimit"/>.</summary>
    public const string LimitOutOfRangeCode = "audit.limit";

    /// <summary>The entries a page holds when the query names no limit.</summary>
    public const int DefaultLimit = 50;

    /// <summary>The most entries a page holds.</summary>
    public const int MaxLimit = 200;

    /// <summary>Creates a query from parts that passed the checks.</summary>
    private AuditQuery(DateTimeOffset? from, DateTimeOffset? to, string? actor, string? target, AuditCursor? before, int limit)
    {
        From = from;
        To = to;
        Actor = actor;
        Target = target;
        Before = before;
        Limit = limit;
    }

    /// <summary>Only entries at or after this moment, in UTC; no lower bound when <see langword="null"/>.</summary>
    public DateTimeOffset? From { get; }

    /// <summary>Only entries before this moment, in UTC; no upper bound when <see langword="null"/>.</summary>
    public DateTimeOffset? To { get; }

    /// <summary>Only entries of the operator with this subject; every operator's when <see langword="null"/>.</summary>
    public string? Actor { get; }

    /// <summary>Only entries about this target; every target's when <see langword="null"/>.</summary>
    public string? Target { get; }

    /// <summary>Only entries older than this one; the newest when <see langword="null"/>.</summary>
    public AuditCursor? Before { get; }

    /// <summary>The most entries the page holds.</summary>
    public int Limit { get; }

    /// <summary>A query that passed the checks, or the error of the first that failed.</summary>
    /// <param name="from">The first moment, in any offset, or <see langword="null"/>.</param>
    /// <param name="to">The first moment after the period, in any offset, or <see langword="null"/>.</param>
    /// <param name="actor">The operator's subject; empty or <see langword="null"/> for every operator.</param>
    /// <param name="target">The target; empty or <see langword="null"/> for every target.</param>
    /// <param name="before">The cursor of the page before, or <see langword="null"/> for the first page.</param>
    /// <param name="limit">The most entries the page holds, or <see langword="null"/> for <see cref="DefaultLimit"/>.</param>
    public static Result<AuditQuery> Create(DateTimeOffset? from, DateTimeOffset? to, string? actor, string? target, string? before, int? limit)
    {
        if (from is { } start && to is { } end && end <= start)
        {
            return Result<AuditQuery>.Failure(new Error(EmptyPeriodCode, "The period ends after it starts."));
        }

        if (actor?.Length > AuditEntry.MaxFieldLength)
        {
            return Result<AuditQuery>.Failure(new Error(ActorTooLongCode, $"An actor has at most {AuditEntry.MaxFieldLength} characters."));
        }

        if (target?.Length > AuditEntry.MaxFieldLength)
        {
            return Result<AuditQuery>.Failure(new Error(TargetTooLongCode, $"A target has at most {AuditEntry.MaxFieldLength} characters."));
        }

        AuditCursor? cursor = null;
        if (!string.IsNullOrEmpty(before) && !AuditCursor.TryParse(before, out cursor))
        {
            return Result<AuditQuery>.Failure(new Error(InvalidCursorCode, "The cursor is not one a page of the audit log handed out."));
        }

        if (limit is < 1 or > MaxLimit)
        {
            return Result<AuditQuery>.Failure(new Error(LimitOutOfRangeCode, $"A page holds between 1 and {MaxLimit} entries."));
        }

        return Result<AuditQuery>.Success(new AuditQuery(
            from?.ToUniversalTime(),
            to?.ToUniversalTime(),
            string.IsNullOrEmpty(actor) ? null : actor,
            string.IsNullOrEmpty(target) ? null : target,
            cursor,
            limit ?? DefaultLimit));
    }
}
