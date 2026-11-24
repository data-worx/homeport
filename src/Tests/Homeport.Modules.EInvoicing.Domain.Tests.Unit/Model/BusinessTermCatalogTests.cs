// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using System.Text.RegularExpressions;
using Homeport.Modules.EInvoicing.Domain.Model;
using Homeport.Testing;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Model;

public sealed class BusinessTermCatalogTests
{
    public sealed class LoadEmbedded
    {
        private readonly IReadOnlyList<BusinessTerm> catalog = BusinessTermCatalog.LoadEmbedded();

        [Fact]
        public void Lists_The_164_Business_Terms_And_32_Business_Groups_Of_En_16931()
        {
            Assert.Equal((164, 32), (catalog.Count(term => !term.IsGroup), catalog.Count(term => term.IsGroup)));
        }

        [Fact]
        public void Lists_Each_Term_And_Group_Once()
        {
            Assert.Equal(catalog.Count, catalog.Select(term => term.Id).Distinct().Count());
        }

        [Fact]
        public void Has_Exactly_One_Member_In_The_Model_For_Every_Term_And_Group()
        {
            var modelled = ModelMembers().Select(member => member.Id).Order().ToList();

            Assert.Equal(catalog.Select(term => term.Id).Order(), modelled);
        }

        [Fact]
        public void Places_Every_Member_In_The_Group_Of_Its_Term()
        {
            var expected = catalog.ToDictionary(term => term.Id, term => term.Group);

            Assert.All(ModelMembers(), member => Assert.Equal(expected[member.Id], member.Group));
        }

        [Fact]
        public void Makes_A_Member_A_List_Exactly_Where_Its_Term_Repeats()
        {
            var expected = catalog.ToDictionary(term => term.Id, term => term.Repeats);

            Assert.All(ModelMembers(), member => Assert.Equal(expected[member.Id], member.IsList));
        }

        [Fact]
        public void Gives_Every_Member_Of_A_Term_The_Type_Of_Its_Data_Type()
        {
            var expected = catalog.Where(term => !term.IsGroup).ToDictionary(term => term.Id, term => ClrTypeOf(term.Type!));

            Assert.All(ModelMembers().Where(member => expected.ContainsKey(member.Id)), member => Assert.Equal(expected[member.Id], member.Type));
        }

        [Fact]
        public void Is_Mapped_To_A_Field_By_The_Page_On_Reading_An_Invoice_For_Every_Term_And_Group()
        {
            var page = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "docs", "api-consumers", "reading-an-invoice.md"));

            var mapped = Regex.Matches(page, @"^\| `invoice\.[^`]+` \| (?<id>B[GT]-\d+) \|", RegexOptions.Multiline).Select(match => match.Groups["id"].Value).Order();

            Assert.Equal(catalog.Select(term => term.Id).Order(), mapped);
        }

        /// <summary>The type a member of the model holds for a data type of EN 16931.</summary>
        private static Type ClrTypeOf(string dataType) => dataType switch
        {
            "identifier" => typeof(Identifier),
            "date" => typeof(DateOnly),
            "binaryObject" => typeof(BinaryObject),
            _ => typeof(string),
        };

        /// <summary>Every member of the model that names a term or group, reached from <see cref="Invoice"/>.</summary>
        private static List<ModelMember> ModelMembers()
        {
            var members = new List<ModelMember>();
            Collect(typeof(Invoice), group: null, members);

            return members;
        }

        private static void Collect(Type type, string? group, List<ModelMember> members)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<BusinessTermAttribute>() is not { } term)
                {
                    continue;
                }

                var isList = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
                var held = isList
                    ? property.PropertyType.GetGenericArguments()[0]
                    : Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                members.Add(new ModelMember(term.Id, group, isList, held));

                if (term.Id.StartsWith("BG-", StringComparison.Ordinal))
                {
                    Collect(held, term.Id, members);
                }
            }
        }

        private sealed record ModelMember(string Id, string? Group, bool IsList, Type Type);
    }
}
