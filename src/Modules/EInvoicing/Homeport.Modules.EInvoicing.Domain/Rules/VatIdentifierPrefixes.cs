// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The country prefixes BR-CO-09 accepts at the start of a VAT identifier.</summary>
/// <remarks>
/// The list is data embedded in the module, <c>vat-identifier-prefixes.json</c>,
/// taken unchanged from the schematron of the EN 16931 validation artefacts
/// and versioned with the module: ISO 3166-1 alpha-2 codes with <c>1A</c> and
/// <c>XI</c>, as the schematron lists them.
/// </remarks>
public static class VatIdentifierPrefixes
{
    private const string ResourceName = "Homeport.Modules.EInvoicing.Domain.Rules.vat-identifier-prefixes.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Every accepted prefix, compared exactly.</summary>
    public static IReadOnlySet<string> All { get; } = LoadEmbedded();

    /// <summary>The prefixes embedded in the module.</summary>
    /// <exception cref="InvalidOperationException">The embedded list is missing or empty, a defect of the build.</exception>
    private static HashSet<string> LoadEmbedded()
    {
        using var stream = typeof(VatIdentifierPrefixes).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The module carries no embedded resource '{ResourceName}'.");
        var file = JsonSerializer.Deserialize<PrefixFile>(stream, JsonOptions);

        return file is { Prefixes.Count: > 0 }
            ? file.Prefixes.ToHashSet(StringComparer.Ordinal)
            : throw new InvalidOperationException("The embedded list of VAT identifier prefixes is empty.");
    }

    /// <summary>The shape of <c>vat-identifier-prefixes.json</c>.</summary>
    /// <param name="Rule">The rule the list belongs to; the module itself does not read it.</param>
    /// <param name="Prefixes">The prefixes, in the order the schematron lists them.</param>
    private sealed record PrefixFile(string Rule, IReadOnlyList<string> Prefixes);
}
