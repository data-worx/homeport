// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Schemas;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Schemas;

public sealed class SchemaCatalogTests
{
    public sealed class LoadEmbedded
    {
        [Theory]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2")]
        [InlineData("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100")]
        public void Compiles_The_Schema_Of_Every_Syntax(string targetNamespace)
        {
            var catalog = SchemaCatalog.LoadEmbedded();

            Assert.True(catalog.Schemas.IsCompiled && catalog.Schemas.Contains(targetNamespace));
        }
    }
}
