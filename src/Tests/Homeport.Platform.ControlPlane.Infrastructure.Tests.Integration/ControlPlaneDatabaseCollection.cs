// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration;

/// <summary>
/// One <see cref="ControlPlaneDatabase"/> for every test class that names it:
/// each test works on tenants of its own.
/// </summary>
[CollectionDefinition(ControlPlaneDatabase.Collection)]
public sealed class ControlPlaneDatabaseCollection : ICollectionFixture<ControlPlaneDatabase>;
