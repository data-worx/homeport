// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The Shared projects <c>Homeport.Sdk</c> may reference (ADR-0018). Each one
/// is shipped with the SDK, so an entry adds to what Homeport ships.
/// </summary>
internal static class SdkReferences
{
    internal static readonly string[] Allowed =
    [
        "Homeport.Shared.Client",
        "Homeport.Shared.Contracts",
        "Homeport.Shared.ServiceDefaults",
    ];
}
