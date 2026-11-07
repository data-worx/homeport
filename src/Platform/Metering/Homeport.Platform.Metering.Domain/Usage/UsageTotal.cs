// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>What a tenant used of one module's meter in one bucket of an export.</summary>
/// <param name="Period">The start of the bucket, in UTC.</param>
/// <param name="ModuleId">The module.</param>
/// <param name="Meter">The meter.</param>
/// <param name="Quantity">The sum of the quantities of the bucket's events.</param>
public sealed record UsageTotal(DateTimeOffset Period, string ModuleId, string Meter, long Quantity);
