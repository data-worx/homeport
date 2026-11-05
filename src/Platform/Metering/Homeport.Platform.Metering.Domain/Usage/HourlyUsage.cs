// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>What a tenant used of one module's meter in one UTC hour.</summary>
/// <param name="TenantId">The tenant.</param>
/// <param name="ModuleId">The module.</param>
/// <param name="Meter">The meter.</param>
/// <param name="Hour">The start of the hour, as <see cref="UsageBuckets.HourOf"/> returns it.</param>
/// <param name="Quantity">The sum of the quantities of the hour's events.</param>
public sealed record HourlyUsage(Guid TenantId, string ModuleId, string Meter, DateTimeOffset Hour, long Quantity);
