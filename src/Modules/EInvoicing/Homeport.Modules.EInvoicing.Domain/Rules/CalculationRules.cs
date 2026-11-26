// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The calculation rules of EN 16931 (BR-CO-nn) as the EN 16931 validation
/// artefacts test them, one method per rule, which returns the JSON path of
/// every place the invoice breaks it.
/// </summary>
/// <remarks>
/// The rules are checked in the document, with the XPath arithmetic of
/// <see cref="XPathNumbers"/>: the schematron of each syntax computes and
/// compares in its own way — rounds or does not, compares as decimal or as
/// double, asks for a value or for its element — and the method follows
/// each syntax's own test, so the module reports what the KoSIT validator
/// reports.
/// </remarks>
public static partial class CalculationRules
{
    /// <summary>Every calculation rule, by its identifier.</summary>
    public static IReadOnlyDictionary<string, Func<RuleInput, IEnumerable<string>>> All { get; } =
        new Dictionary<string, Func<RuleInput, IEnumerable<string>>>(StringComparer.Ordinal)
        {
            ["BR-CO-03"] = BrCo03,
            ["BR-CO-04"] = BrCo04,
            ["BR-CO-05"] = BrCo05,
            ["BR-CO-06"] = BrCo06,
            ["BR-CO-07"] = BrCo07,
            ["BR-CO-08"] = BrCo08,
            ["BR-CO-09"] = BrCo09,
            ["BR-CO-10"] = BrCo10,
            ["BR-CO-11"] = BrCo11,
            ["BR-CO-12"] = BrCo12,
            ["BR-CO-13"] = BrCo13,
            ["BR-CO-14"] = BrCo14,
            ["BR-CO-15"] = BrCo15,
            ["BR-CO-16"] = BrCo16,
            ["BR-CO-17"] = BrCo17,
            ["BR-CO-18"] = BrCo18,
            ["BR-CO-19"] = BrCo19,
            ["BR-CO-20"] = BrCo20,
            ["BR-CO-21"] = BrCo21,
            ["BR-CO-22"] = BrCo22,
            ["BR-CO-23"] = BrCo23,
            ["BR-CO-24"] = BrCo24,
            ["BR-CO-26"] = BrCo26,
        };

    /// <summary>The path, when the rule is broken; nothing when it holds.</summary>
    private static string[] BrokenAt(bool broken, string path) => broken ? [path] : [];
}
