// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>Where a project lives under <c>src/</c>, which tells the tests what it is.</summary>
internal enum ProjectLocation
{
    /// <summary><c>src/Aspire/&lt;project&gt;/</c> - the AppHost, which orchestrates the deployables.</summary>
    Aspire,

    /// <summary><c>src/Shared/&lt;project&gt;/</c> - code every service may build on, such as the contracts.</summary>
    Shared,

    /// <summary><c>src/Sdk/&lt;project&gt;/</c> - the module SDK.</summary>
    Sdk,

    /// <summary><c>src/Platform/&lt;Service&gt;/&lt;project&gt;/</c> - a project of a platform service.</summary>
    Platform,

    /// <summary><c>src/Modules/&lt;Module&gt;/&lt;project&gt;/</c> - a project of a business module.</summary>
    Modules,

    /// <summary><c>src/Tools/&lt;project&gt;/</c> - a development tool.</summary>
    Tools,

    /// <summary><c>src/Tests/&lt;project&gt;/</c> - a test project.</summary>
    Tests,

    /// <summary>Anywhere else, which the layout does not allow.</summary>
    Elsewhere,
}
