// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Metering;

/// <summary>Where <see cref="UsageRecorded"/> travels through RabbitMQ.</summary>
public static class UsageMessaging
{
    /// <summary>The fanout exchange the gateway publishes to.</summary>
    public const string Exchange = "homeport.usage";

    /// <summary>The durable queue metering consumes from, bound to <see cref="Exchange"/>.</summary>
    public const string MeteringQueue = "homeport.metering.usage";
}
