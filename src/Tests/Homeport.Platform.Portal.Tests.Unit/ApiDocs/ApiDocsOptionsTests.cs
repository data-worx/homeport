// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.ApiDocs;

namespace Homeport.Platform.Portal.Tests.Unit.ApiDocs;

public sealed class ApiDocsOptionsTests
{
    public sealed class DocumentOf
    {
        [Theory]
        [InlineData("https://api.example.com/platform/docs")]
        [InlineData("https://api.example.com/platform/docs/")]
        public void Appends_The_Module_Id_To_The_Docs(string docs)
        {
            var options = new ApiDocsOptions { Url = new Uri(docs) };

            Assert.Equal(new Uri("https://api.example.com/platform/docs/echo"), options.DocumentOf("echo"));
        }

        [Fact]
        public void Returns_Nothing_Without_The_Docs()
        {
            Assert.Null(new ApiDocsOptions().DocumentOf("echo"));
        }
    }
}
