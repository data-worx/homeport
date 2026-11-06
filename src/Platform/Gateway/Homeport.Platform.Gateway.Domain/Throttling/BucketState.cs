// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Domain.Throttling;

/// <summary>A tenant's bucket right after a request tried to take a token from it.</summary>
/// <param name="Taken">Whether the request got a token.</param>
/// <param name="Remaining">The whole tokens left in the bucket.</param>
/// <param name="NextToken">How long until the bucket holds a token again; zero while it holds one.</param>
/// <param name="Full">How long until the bucket is full again; zero while it is.</param>
public sealed record BucketState(bool Taken, int Remaining, TimeSpan NextToken, TimeSpan Full);
