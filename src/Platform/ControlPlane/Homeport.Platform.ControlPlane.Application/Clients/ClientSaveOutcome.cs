// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Clients;

/// <summary>What became of the changes <see cref="IClientStore.TrySaveAsync"/> stored.</summary>
public enum ClientSaveOutcome
{
    /// <summary>Every change was stored.</summary>
    Saved,

    /// <summary>The same client id was registered meanwhile; nothing was stored.</summary>
    ClientIdTaken,

    /// <summary>The plan was deleted meanwhile; nothing was stored.</summary>
    PlanUnknown,
}
