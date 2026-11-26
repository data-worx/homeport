// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The code lists the code-list rules of EN 16931 (BR-CL-nn) check a code against.</summary>
/// <remarks>
/// The lists are data embedded in the module, <c>code-lists.json</c>, taken
/// unchanged from the schematron of the EN 16931 validation artefacts and
/// versioned with the module; an operator cannot override them. Where the
/// schematron of UBL and of CII test a rule against different lists, each
/// syntax has a list of its own.
/// </remarks>
public static class CodeLists
{
    /// <summary>The invoice type codes of UNTDID 1001 a UBL Invoice may carry (BR-CL-01).</summary>
    public const string Untdid1001UblInvoice = "UNTDID 1001 (UBL invoice)";

    /// <summary>The credit note type codes of UNTDID 1001 a UBL CreditNote may carry (BR-CL-01).</summary>
    public const string Untdid1001UblCreditNote = "UNTDID 1001 (UBL credit note)";

    /// <summary>The document type codes of UNTDID 1001 a CII invoice may carry (BR-CL-01).</summary>
    public const string Untdid1001Cii = "UNTDID 1001 (CII)";

    /// <summary>The currency codes of ISO 4217 (BR-CL-03, BR-CL-04, BR-CL-05).</summary>
    public const string Iso4217 = "ISO 4217";

    /// <summary>The VAT point date codes of UNTDID 2005 UBL accepts (BR-CL-06).</summary>
    public const string Untdid2005Ubl = "UNTDID 2005 (UBL)";

    /// <summary>The VAT point date codes of UNTDID 2005 CII accepts (BR-CL-06).</summary>
    public const string Untdid2005Cii = "UNTDID 2005 (CII)";

    /// <summary>The reference qualifiers of UNTDID 1153 (BR-CL-07).</summary>
    public const string Untdid1153 = "UNTDID 1153";

    /// <summary>The text subject qualifiers of UNTDID 4451 (BR-CL-08).</summary>
    public const string Untdid4451 = "UNTDID 4451";

    /// <summary>The identification schemes of ISO 6523 ICD (BR-CL-10, BR-CL-11, BR-CL-21, BR-CL-26).</summary>
    public const string Iso6523Icd = "ISO 6523 ICD";

    /// <summary>The item type identification codes of UNTDID 7143 (BR-CL-13).</summary>
    public const string Untdid7143 = "UNTDID 7143";

    /// <summary>The country codes of ISO 3166-1 alpha-2, with <c>1A</c> and <c>XI</c> (BR-CL-14, BR-CL-15).</summary>
    public const string Iso3166 = "ISO 3166-1";

    /// <summary>The payment means codes of UNTDID 4461 (BR-CL-16).</summary>
    public const string Untdid4461 = "UNTDID 4461";

    /// <summary>The VAT category codes of UNTDID 5305 (BR-CL-17, BR-CL-18).</summary>
    public const string Untdid5305 = "UNTDID 5305";

    /// <summary>The allowance reason codes of UNTDID 5189 (BR-CL-19).</summary>
    public const string Untdid5189 = "UNTDID 5189";

    /// <summary>The charge reason codes of UNTDID 7161 (BR-CL-20).</summary>
    public const string Untdid7161 = "UNTDID 7161";

    /// <summary>The VAT exemption reason codes of the VATEX list (BR-CL-22).</summary>
    public const string Vatex = "VATEX";

    /// <summary>The units of measure of UNECE Recommendations 20 and 21 (BR-CL-23).</summary>
    public const string UnitsOfMeasure = "UNECE Recommendation 20 and 21";

    /// <summary>The MIME media types of an attached document (BR-CL-24).</summary>
    public const string MimeMediaTypes = "MIME media types";

    /// <summary>The electronic address schemes of the EAS list (BR-CL-25).</summary>
    public const string Eas = "EAS";

    private const string ResourceName = "Homeport.Modules.EInvoicing.Domain.Rules.code-lists.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Every list, by its name.</summary>
    public static IReadOnlyDictionary<string, CodeList> All { get; } = LoadEmbedded();

    /// <summary>The list of the given name.</summary>
    /// <param name="name">One of the names this class declares.</param>
    /// <exception cref="KeyNotFoundException">The module carries no list of that name, a defect of the build.</exception>
    public static CodeList Named(string name) => All[name];

    /// <summary>The lists embedded in the module.</summary>
    /// <exception cref="InvalidOperationException">The embedded lists are missing or empty, or a name occurs twice, a defect of the build.</exception>
    private static Dictionary<string, CodeList> LoadEmbedded()
    {
        using var stream = typeof(CodeLists).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The module carries no embedded resource '{ResourceName}'.");
        var file = JsonSerializer.Deserialize<CodeListFile>(stream, JsonOptions);
        if (file is not { Lists.Count: > 0 } || file.Lists.Any(list => list.Codes.Count == 0))
        {
            throw new InvalidOperationException("The embedded code lists are empty.");
        }

        var lists = new Dictionary<string, CodeList>(StringComparer.Ordinal);
        foreach (var entry in file.Lists)
        {
            if (!lists.TryAdd(entry.Name, new CodeList(entry.Name, entry.Codes)))
            {
                throw new InvalidOperationException($"The embedded code list '{entry.Name}' occurs twice.");
            }
        }

        return lists;
    }

    /// <summary>The shape of <c>code-lists.json</c>.</summary>
    /// <param name="Source">Where the lists are taken from; the module itself does not read it.</param>
    /// <param name="Lists">The lists.</param>
    private sealed record CodeListFile(string Source, IReadOnlyList<CodeListEntry> Lists);

    /// <summary>A list in <c>code-lists.json</c>.</summary>
    /// <param name="Name">See <see cref="CodeList.Name"/>.</param>
    /// <param name="Rules">The rules that check against the list; the module itself does not read it.</param>
    /// <param name="Codes">The codes, in the order the schematron lists them.</param>
    private sealed record CodeListEntry(string Name, IReadOnlyList<string> Codes, IReadOnlyList<string> Rules);
}
