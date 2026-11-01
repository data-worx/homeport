// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.Registry;

namespace Homeport.Platform.Gateway.Infrastructure.Registry;

/// <summary>A route table as read from the registry.</summary>
/// <param name="Table">The routes.</param>
/// <param name="ETag">Its <c>ETag</c>, sent back with the next read; <c>null</c> when the registry sent none.</param>
public sealed record RouteTableRead(RouteTableResponse Table, string? ETag);
