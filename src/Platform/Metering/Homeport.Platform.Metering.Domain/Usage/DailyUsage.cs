// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>What a tenant used of one module's meter on one UTC day.</summary>
/// <param name="TenantId">The tenant.</param>
/// <param name="ModuleId">The module.</param>
/// <param name="Meter">The meter.</param>
/// <param name="Day">The day, as <see cref="UsageBuckets.DayOf"/> returns it.</param>
/// <param name="Quantity">The sum of the quantities of the day's events.</param>
public sealed record DailyUsage(Guid TenantId, string ModuleId, string Meter, DateOnly Day, long Quantity);
