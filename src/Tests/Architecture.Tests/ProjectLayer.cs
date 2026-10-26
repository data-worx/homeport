// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// What a project of a service or module is, read from its name. Declared from
/// the centre outwards, so ordering by it reads like the dependency arrow.
/// </summary>
internal enum ProjectLayer
{
    /// <summary>Not part of a service or module.</summary>
    None,

    /// <summary><c>Homeport.&lt;Service&gt;</c> - a service without business rules, in one project.</summary>
    Single,

    /// <summary><c>.Domain</c> - the models and the rules on them.</summary>
    Domain,

    /// <summary><c>.Application</c> - the use cases.</summary>
    Application,

    /// <summary><c>.Infrastructure</c> - what the use cases need from the outside world.</summary>
    Infrastructure,

    /// <summary><c>.Api</c> - the HTTP host and composition root.</summary>
    Api,

    /// <summary><c>.Worker</c> - the optional background host and composition root.</summary>
    Worker,

    /// <summary>A name that does not fit the service or module it lies in.</summary>
    Unknown,
}
