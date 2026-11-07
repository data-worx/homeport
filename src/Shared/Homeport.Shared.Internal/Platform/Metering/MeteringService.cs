// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Metering;

/// <summary>How the gateway reaches metering's HTTP API.</summary>
public static class MeteringService
{
    /// <summary>Metering's service name, as the AppHost declares it.</summary>
    public const string ServiceName = "metering";

    /// <summary>The audience of the tokens metering accepts (ADR-0014, ADR-0019).</summary>
    public const string Audience = "homeport-metering";
}
