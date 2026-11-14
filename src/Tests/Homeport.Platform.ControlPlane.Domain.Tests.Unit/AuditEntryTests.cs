// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain.Tests.Unit;

public sealed class AuditEntryTests
{
    public sealed class ClientTarget
    {
        [Fact]
        public void Fits_The_Longest_Client_Id_Exactly_Into_The_Longest_Target()
        {
            var target = AuditEntry.ClientTarget(new string('a', Client.MaxClientIdLength));

            Assert.Equal(AuditEntry.MaxTargetLength, target.Length);
        }
    }
}
