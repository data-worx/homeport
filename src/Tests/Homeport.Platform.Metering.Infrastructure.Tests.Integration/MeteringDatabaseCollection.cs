// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration;

/// <summary>
/// One <see cref="MeteringDatabase"/> for every test class that names it: none
/// of them changes the rows of the fixture's tenants.
/// </summary>
[CollectionDefinition(MeteringDatabase.Collection)]
public sealed class MeteringDatabaseCollection : ICollectionFixture<MeteringDatabase>;
