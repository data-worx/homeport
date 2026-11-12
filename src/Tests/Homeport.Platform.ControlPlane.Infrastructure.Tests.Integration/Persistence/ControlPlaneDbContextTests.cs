// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Testing.Persistence;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

public sealed class ControlPlaneDbContextTests
{
    public sealed class Model
    {
        private const string Diagram = "data-model-control-plane.drawio.svg";

        [Fact]
        public void Names_Every_Table_And_Column_In_The_Er_Diagram()
        {
            using var context = new ControlPlaneDesignTimeDbContextFactory().CreateDbContext([]);

            var missing = ErDiagram.Read(Diagram).MissingFrom(context);

            Assert.True(missing.Count == 0, $"{Diagram} lacks: {string.Join(", ", missing)}");
        }
    }
}
