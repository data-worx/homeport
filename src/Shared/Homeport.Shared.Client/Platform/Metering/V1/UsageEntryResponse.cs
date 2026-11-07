// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Metering.V1;

/// <summary>What a tenant used of one module's meter in one bucket.</summary>
/// <param name="Period">The start of the hour or day, in UTC.</param>
/// <param name="Module">The module id.</param>
/// <param name="Meter">The meter.</param>
/// <param name="Quantity">The sum of the quantities counted in the bucket.</param>
public sealed record UsageEntryResponse(DateTime Period, string Module, string Meter, long Quantity);
