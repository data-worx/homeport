// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The last metering migration of each release, the starting point of
/// the upgrade to the next one. A release that changes the schema adds its own.
/// </summary>
internal static class ReleasedMigrations
{
    /// <summary>The last migration of v0.1.0.</summary>
    internal const string V0_1_0 = "20261104212415_EnableRowLevelSecurity";
}
