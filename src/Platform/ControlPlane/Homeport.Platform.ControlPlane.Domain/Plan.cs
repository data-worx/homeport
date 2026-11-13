// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>The limits a tenant's clients get: a token bucket of requests.</summary>
public sealed class Plan
{
    /// <summary>The longest name of a plan.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The name is empty or only whitespace.</summary>
    public const string NameRequiredCode = "plan.name-required";

    /// <summary>The name is longer than <see cref="MaxNameLength"/>.</summary>
    public const string NameTooLongCode = "plan.name-too-long";

    /// <summary>Another plan has the same name.</summary>
    public const string NameTakenCode = "plan.name-taken";

    /// <summary>The rate is zero or negative.</summary>
    public const string RequestsPerSecondNotPositiveCode = "plan.requests-per-second-not-positive";

    /// <summary>The burst is zero or negative.</summary>
    public const string BurstNotPositiveCode = "plan.burst-not-positive";

    /// <summary>A client uses the plan, so it cannot be deleted.</summary>
    public const string InUseCode = "plan.in-use";

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    /// <summary>How fast the bucket refills.</summary>
    public required int RequestsPerSecond { get; set; }

    /// <summary>How many requests the bucket holds.</summary>
    public required int Burst { get; set; }

    /// <summary>
    /// A new plan named <paramref name="name"/>, without its surrounding
    /// whitespace, unless the name is empty, too long or already taken, or a
    /// limit is not positive.
    /// </summary>
    /// <param name="id">The new plan's id.</param>
    /// <param name="name">The name the operator gave.</param>
    /// <param name="requestsPerSecond">How fast the bucket refills.</param>
    /// <param name="burst">How many requests the bucket holds; it may be below the rate.</param>
    /// <param name="takenNames">The names of every existing plan.</param>
    public static Result<Plan> Create(Guid id, string name, int requestsPerSecond, int burst, IEnumerable<string> takenNames)
    {
        var checkedName = Check(name, requestsPerSecond, burst, takenNames);

        return checkedName.IsSuccess
            ? Result<Plan>.Success(new Plan { Id = id, Name = checkedName.Value, RequestsPerSecond = requestsPerSecond, Burst = burst })
            : Result<Plan>.Failure(checkedName.Error);
    }

    /// <summary>
    /// Gives the plan the name <paramref name="name"/>, without its surrounding
    /// whitespace, and the limits given, unless the name is empty, too long or
    /// taken by another plan, or a limit is not positive.
    /// </summary>
    /// <param name="name">The name the operator gave.</param>
    /// <param name="requestsPerSecond">How fast the bucket refills.</param>
    /// <param name="burst">How many requests the bucket holds; it may be below the rate.</param>
    /// <param name="takenNames">The names of every other plan.</param>
    /// <returns>The changed plan, or why it was not changed.</returns>
    public Result<Plan> Change(string name, int requestsPerSecond, int burst, IEnumerable<string> takenNames)
    {
        var checkedName = Check(name, requestsPerSecond, burst, takenNames);
        if (!checkedName.IsSuccess)
        {
            return Result<Plan>.Failure(checkedName.Error);
        }

        Name = checkedName.Value;
        RequestsPerSecond = requestsPerSecond;
        Burst = burst;
        return Result<Plan>.Success(this);
    }

    /// <summary>
    /// The plan, when it may be deleted: only while no client uses it, since
    /// every client is held to exactly one plan.
    /// </summary>
    /// <param name="clientCount">How many clients use the plan.</param>
    public Result<Plan> CheckDeletable(int clientCount)
    {
        return clientCount == 0 ? Result<Plan>.Success(this) : Result<Plan>.Failure(InUse(Name));
    }

    /// <summary>The error of a name that another plan has.</summary>
    public static Error NameTaken(string name)
    {
        return new Error(NameTakenCode, $"A plan named '{name}' already exists.");
    }

    /// <summary>The error of a plan that a client uses.</summary>
    public static Error InUse(string name)
    {
        return new Error(InUseCode, $"The plan '{name}' is used by at least one client. Move its clients to another plan first.");
    }

    // Unique compared case-sensitively: "Gold" and "gold" are two plans.
    private static Result<string> Check(string name, int requestsPerSecond, int burst, IEnumerable<string> takenNames)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(new Error(NameRequiredCode, "A plan needs a name."));
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result<string>.Failure(new Error(NameTooLongCode, $"A plan's name has at most {MaxNameLength} characters."));
        }

        if (requestsPerSecond <= 0)
        {
            return Result<string>.Failure(new Error(RequestsPerSecondNotPositiveCode, "The requests per second must be at least 1."));
        }

        if (burst <= 0)
        {
            return Result<string>.Failure(new Error(BurstNotPositiveCode, "The burst must be at least 1."));
        }

        if (takenNames.Contains(trimmed, StringComparer.Ordinal))
        {
            return Result<string>.Failure(NameTaken(trimmed));
        }

        return Result<string>.Success(trimmed);
    }
}
