// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Metering.V1;

/// <summary>A tenant's usage in a period, one entry per bucket, module and meter.</summary>
/// <param name="From">The first moment of the period, in UTC.</param>
/// <param name="To">The first moment after the period, in UTC.</param>
/// <param name="Granularity"><see cref="UsageApi.Hour"/> or <see cref="UsageApi.Day"/>.</param>
/// <param name="Usage">Every bucket that starts in the period and has usage, ordered by period, module and meter.</param>
public sealed record UsageResponse(DateTime From, DateTime To, string Granularity, IReadOnlyList<UsageEntryResponse> Usage);
