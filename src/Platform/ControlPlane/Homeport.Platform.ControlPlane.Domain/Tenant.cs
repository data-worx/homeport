// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>A department or customer of the operator: the unit of isolation and of metering.</summary>
public sealed class Tenant
{
    /// <summary>The longest name of a tenant.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The name is empty or only whitespace.</summary>
    public const string NameRequiredCode = "tenant.name-required";

    /// <summary>The name is longer than <see cref="MaxNameLength"/>.</summary>
    public const string NameTooLongCode = "tenant.name-too-long";

    /// <summary>Another tenant has the same name.</summary>
    public const string NameTakenCode = "tenant.name-taken";

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    /// <summary>
    /// A new tenant named <paramref name="name"/>, without its surrounding
    /// whitespace, unless the name is empty, too long or already taken.
    /// </summary>
    /// <param name="id">The new tenant's id.</param>
    /// <param name="name">The name the operator gave.</param>
    /// <param name="takenNames">The names of every existing tenant.</param>
    public static Result<Tenant> Create(Guid id, string name, IEnumerable<string> takenNames)
    {
        var checkedName = CheckName(name, takenNames);

        return checkedName.IsSuccess
            ? Result<Tenant>.Success(new Tenant { Id = id, Name = checkedName.Value })
            : Result<Tenant>.Failure(checkedName.Error);
    }

    /// <summary>
    /// Renames the tenant to <paramref name="name"/>, without its surrounding
    /// whitespace, unless the name is empty, too long or taken by another tenant.
    /// </summary>
    /// <param name="name">The name the operator gave.</param>
    /// <param name="takenNames">The names of every other tenant.</param>
    /// <returns>The renamed tenant, or why it was not renamed.</returns>
    public Result<Tenant> Rename(string name, IEnumerable<string> takenNames)
    {
        var checkedName = CheckName(name, takenNames);
        if (!checkedName.IsSuccess)
        {
            return Result<Tenant>.Failure(checkedName.Error);
        }

        Name = checkedName.Value;
        return Result<Tenant>.Success(this);
    }

    /// <summary>The error of a name that another tenant has.</summary>
    public static Error NameTaken(string name)
    {
        return new Error(NameTakenCode, $"A tenant named '{name}' already exists.");
    }

    // Unique compared case-sensitively: "Sales" and "sales" are two tenants.
    private static Result<string> CheckName(string name, IEnumerable<string> takenNames)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(new Error(NameRequiredCode, "A tenant needs a name."));
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result<string>.Failure(new Error(NameTooLongCode, $"A tenant's name has at most {MaxNameLength} characters."));
        }

        if (takenNames.Contains(trimmed, StringComparer.Ordinal))
        {
            return Result<string>.Failure(NameTaken(trimmed));
        }

        return Result<string>.Success(trimmed);
    }
}
