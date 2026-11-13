// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// The answer of the admin API to one request: the value, the problem it
/// answered with, or that the operator has to sign in again first.
/// </summary>
/// <typeparam name="TValue">The type of the value on success.</typeparam>
public sealed class AdminResult<TValue>
{
    private AdminResult(TValue? value, ProblemResponse? problem, bool signInRequired)
    {
        Value = value;
        Problem = problem;
        SignInRequired = signInRequired;
    }

    /// <summary>The value, when the request succeeded.</summary>
    public TValue? Value { get; }

    /// <summary>What went wrong, as Problem Details, when it failed.</summary>
    public ProblemResponse? Problem { get; }

    /// <summary>Whether the operator's session has ended and they have to sign in again.</summary>
    public bool SignInRequired { get; }

    /// <summary>A success with <paramref name="value"/>.</summary>
    public static AdminResult<TValue> Success(TValue value) => new(value, null, signInRequired: false);

    /// <summary>A failure the admin API, or the portal on its behalf, described as <paramref name="problem"/>.</summary>
    public static AdminResult<TValue> Failure(ProblemResponse problem) => new(default, problem, signInRequired: false);

    /// <summary>The operator has to sign in again.</summary>
    public static AdminResult<TValue> SignInAgain() => new(default, null, signInRequired: true);
}
