// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Sdk.Tests.Unit;

public sealed class ProblemTypeTests
{
    public sealed class Constructor
    {
        private static readonly Uri AbsoluteType = new("https://things.example/problems/conflict");

        [Fact]
        public void Refuses_A_Relative_Type()
        {
            Assert.Throws<ArgumentException>(() => new ProblemType(new Uri("/problems/conflict", UriKind.Relative), "Conflict", 409));
        }

        [Fact]
        public void Refuses_An_Empty_Title()
        {
            Assert.Throws<ArgumentException>(() => new ProblemType(AbsoluteType, " ", 409));
        }

        [Theory]
        [InlineData(200)]
        [InlineData(399)]
        [InlineData(600)]
        public void Refuses_A_Status_That_Is_Not_An_Error(int status)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProblemType(AbsoluteType, "Conflict", status));
        }

        [Theory]
        [InlineData(400)]
        [InlineData(599)]
        public void Accepts_An_Error_Status(int status)
        {
            var problem = new ProblemType(AbsoluteType, "Conflict", status);

            Assert.Equal(status, problem.Status);
        }
    }
}
