// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Aspire.AppHost.Tests.Integration;

/// <summary>
/// The test classes that share one <see cref="RunningAppHost"/>. The whole
/// topology is started once per run, and the classes in the collection run
/// one after the other.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AppHostCollection : ICollectionFixture<RunningAppHost>
{
    internal const string Name = "AppHost";
}
