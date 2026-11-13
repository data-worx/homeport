// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Plans;

/// <summary>What became of the changes <see cref="IPlanStore.TrySaveAsync"/> stored.</summary>
public enum PlanSaveOutcome
{
    /// <summary>Every change was stored.</summary>
    Saved,

    /// <summary>Another plan took the name meanwhile; nothing was stored.</summary>
    NameTaken,

    /// <summary>A client started using the plan meanwhile; nothing was stored.</summary>
    InUse,
}
