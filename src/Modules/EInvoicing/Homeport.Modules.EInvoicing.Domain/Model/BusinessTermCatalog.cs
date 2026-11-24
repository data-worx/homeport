// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The business terms and business groups of EN 16931.</summary>
/// <remarks>
/// The catalog is data embedded in the module, <c>business-terms.json</c>.
/// Identifiers, names, groups and data types are taken from the semantic
/// model of the KoSIT XRechnung visualization (<c>xrechnung-semantic-model.xsd</c>,
/// release v2026-10-13), without the extensions of XRechnung. Which terms and
/// groups repeat is taken from it too, except for BG-30, which the syntax
/// rule UBL-SR-48 of the EN 16931 validation artefacts allows once per line.
/// </remarks>
public static class BusinessTermCatalog
{
    private const string ResourceName = "Homeport.Modules.EInvoicing.Domain.Model.business-terms.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Every business term and business group, in the order of EN 16931.</summary>
    /// <exception cref="InvalidOperationException">The embedded catalog is missing or empty, a defect of the build.</exception>
    public static IReadOnlyList<BusinessTerm> LoadEmbedded()
    {
        using var stream = typeof(BusinessTermCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The module carries no embedded resource '{ResourceName}'.");
        var file = JsonSerializer.Deserialize<CatalogFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("The embedded business-term catalog is empty.");

        return file.Terms;
    }

    /// <summary>The shape of <c>business-terms.json</c>.</summary>
    /// <param name="Terms">The terms and groups.</param>
    private sealed record CatalogFile(IReadOnlyList<BusinessTerm> Terms);
}

/// <summary>A business term or business group of EN 16931.</summary>
/// <param name="Id">The identifier, such as <c>BT-1</c> or <c>BG-4</c>.</param>
/// <param name="Name">The name EN 16931 gives it, such as <c>Invoice number</c>.</param>
/// <param name="Group">The group it belongs to; none for one on the level of the invoice.</param>
/// <param name="Repeats">Whether an invoice may carry it more than once where it may carry it.</param>
/// <param name="Type">The data type of a term, such as <c>amount</c>; none for a group.</param>
public sealed record BusinessTerm(string Id, string Name, string? Group, bool Repeats, string? Type)
{
    /// <summary>Whether it is a business group rather than a business term.</summary>
    public bool IsGroup => Type is null;
}
