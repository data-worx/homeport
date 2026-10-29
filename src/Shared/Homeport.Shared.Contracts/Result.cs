// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Contracts;

/// <summary>
/// The outcome of an operation that can fail for a business reason: either a
/// value or an <see cref="Contracts.Error"/>. An expected failure is returned,
/// never thrown.
/// </summary>
/// <typeparam name="TValue">The type of the value on success.</typeparam>
public sealed class Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    private Result(TValue? value, Error? error)
    {
        _value = value;
        _error = error;
    }

    /// <summary>Whether the result holds a value rather than an error.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>The value of a success.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value; its error is '{_error!.Code}'.");

    /// <summary>The error of a failure.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>A result holding <paramref name="value"/>.</summary>
    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(value, null);
    }

    /// <summary>A result holding <paramref name="error"/>.</summary>
    public static Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(default, error);
    }
}
