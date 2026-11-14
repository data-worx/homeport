// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>
/// A machine identity of a tenant, and the plan its requests are held to. The
/// client is created at the identity provider; Homeport maps its client id.
/// </summary>
public sealed class Client
{
    /// <summary>The longest client id; the identity providers Homeport targets stay well below.</summary>
    public const int MaxClientIdLength = 255;

    /// <summary>The client id is empty or only whitespace.</summary>
    public const string ClientIdRequiredCode = "client.id-required";

    /// <summary>The client id is longer than <see cref="MaxClientIdLength"/>.</summary>
    public const string ClientIdTooLongCode = "client.id-too-long";

    /// <summary>The client id is already registered.</summary>
    public const string ClientIdTakenCode = "client.id-taken";

    /// <summary>The tenant does not exist.</summary>
    public const string TenantUnknownCode = "client.tenant-unknown";

    /// <summary>The plan does not exist.</summary>
    public const string PlanUnknownCode = "client.plan-unknown";

    /// <summary>The client id as the identity provider issued it.</summary>
    public required string ClientId { get; init; }

    public required Guid TenantId { get; set; }

    public required Guid PlanId { get; set; }

    public Plan? Plan { get; set; }

    /// <summary>
    /// A new client <paramref name="clientId"/> of <paramref name="tenant"/> on
    /// <paramref name="plan"/>, unless the client id is empty, too long or
    /// already registered, or the tenant or the plan does not exist.
    /// </summary>
    /// <param name="clientId">
    /// The client id exactly as the identity provider issued it. It is not
    /// trimmed: a token names it exactly.
    /// </param>
    /// <param name="tenant">The tenant the client belongs to, or <see langword="null"/> when there is no such tenant.</param>
    /// <param name="plan">The plan the client is held to, or <see langword="null"/> when there is no such plan.</param>
    /// <param name="registered">Whether a client with the same id is already registered.</param>
    public static Result<Client> Register(string clientId, Tenant? tenant, Plan? plan, bool registered)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Result<Client>.Failure(new Error(ClientIdRequiredCode, "A client needs the client id the identity provider issued."));
        }

        if (clientId.Length > MaxClientIdLength)
        {
            return Result<Client>.Failure(new Error(ClientIdTooLongCode, $"A client id has at most {MaxClientIdLength} characters."));
        }

        if (registered)
        {
            return Result<Client>.Failure(ClientIdTaken(clientId));
        }

        if (tenant is null)
        {
            return Result<Client>.Failure(new Error(TenantUnknownCode, "There is no such tenant."));
        }

        if (plan is null)
        {
            return Result<Client>.Failure(PlanUnknown());
        }

        // The ids only: the tenant and the plan are stored already.
        return Result<Client>.Success(new Client { ClientId = clientId, TenantId = tenant.Id, PlanId = plan.Id });
    }

    /// <summary>
    /// Holds the client to <paramref name="plan"/> from now on, unless there is
    /// no such plan. The client keeps its tenant.
    /// </summary>
    /// <param name="plan">The new plan, or <see langword="null"/> when there is no such plan.</param>
    /// <returns>The changed client, or why it was not changed.</returns>
    public Result<Client> ChangePlan(Plan? plan)
    {
        if (plan is null)
        {
            return Result<Client>.Failure(PlanUnknown());
        }

        PlanId = plan.Id;
        return Result<Client>.Success(this);
    }

    /// <summary>The error of a client id that is already registered.</summary>
    public static Error ClientIdTaken(string clientId)
    {
        return new Error(ClientIdTakenCode, $"The client '{clientId}' is already registered.");
    }

    /// <summary>The error of a plan that does not exist.</summary>
    public static Error PlanUnknown()
    {
        return new Error(PlanUnknownCode, "There is no such plan.");
    }
}
