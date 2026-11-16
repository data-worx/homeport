// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>What one tenant used of one module's meter in one bucket, for the operator.</summary>
/// <param name="TenantId">The tenant.</param>
/// <param name="Period">The start of the bucket, in UTC.</param>
/// <param name="ModuleId">The module.</param>
/// <param name="Meter">The meter.</param>
/// <param name="Quantity">The sum of the quantities of the bucket's events.</param>
public sealed record TenantUsageTotal(Guid TenantId, DateTimeOffset Period, string ModuleId, string Meter, long Quantity);
