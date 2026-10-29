// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Contracts.Tests.Unit;

public sealed class ResultTests
{
    private static readonly Error NotFound = new("order.not-found", "The order does not exist.");

    public sealed class Success
    {
        [Fact]
        public void Is_A_Success()
        {
            var result = Result<int>.Success(42);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Holds_The_Value()
        {
            var result = Result<int>.Success(42);

            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Has_No_Error()
        {
            var result = Result<int>.Success(42);

            Assert.Throws<InvalidOperationException>(() => result.Error);
        }
    }

    public sealed class Failure
    {
        [Fact]
        public void Is_Not_A_Success()
        {
            var result = Result<int>.Failure(NotFound);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Holds_The_Error()
        {
            var result = Result<int>.Failure(NotFound);

            Assert.Same(NotFound, result.Error);
        }

        [Fact]
        public void Has_No_Value()
        {
            var result = Result<int>.Failure(NotFound);

            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void Rejects_A_Missing_Error()
        {
            Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
        }
    }
}
