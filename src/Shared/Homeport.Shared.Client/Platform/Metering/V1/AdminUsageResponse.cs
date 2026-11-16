// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Metering.V1;

/// <summary>The usage of every tenant, or of one, in a period: one entry per bucket, tenant, module and meter.</summary>
/// <param name="From">The first moment of the period, in UTC.</param>
/// <param name="To">The first moment after the period, in UTC.</param>
/// <param name="Granularity"><see cref="UsageApi.Hour"/> or <see cref="UsageApi.Day"/>.</param>
/// <param name="Usage">Every bucket that starts in the period and has usage, ordered by period, tenant, module and meter.</param>
public sealed record AdminUsageResponse(DateTime From, DateTime To, string Granularity, IReadOnlyList<AdminUsageEntryResponse> Usage);
