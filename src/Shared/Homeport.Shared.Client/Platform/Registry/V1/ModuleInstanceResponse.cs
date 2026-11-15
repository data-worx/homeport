// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Registry.V1;

/// <summary>A live instance of a module.</summary>
/// <param name="InstanceId">Unique per running instance; chosen by the instance.</param>
/// <param name="Address">Where the gateway reaches the instance.</param>
/// <param name="Version">The module version the instance runs.</param>
/// <param name="ContractVersion">The version of the module contract the instance implements.</param>
/// <param name="LastHeartbeatAt">When the instance last registered or sent a heartbeat.</param>
/// <param name="ExpiresAt">When the registration ends unless another heartbeat arrives.</param>
public sealed record ModuleInstanceResponse(
    string InstanceId,
    Uri Address,
    string Version,
    int ContractVersion,
    DateTimeOffset LastHeartbeatAt,
    DateTimeOffset ExpiresAt);
