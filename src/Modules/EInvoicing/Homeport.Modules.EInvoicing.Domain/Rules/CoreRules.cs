// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The core rules of EN 16931 (BR-nn) as the EN 16931 validation artefacts
/// test them, one method per rule, which returns the JSON path of every place
/// the invoice breaks it.
/// </summary>
/// <remarks>
/// A rule serves UBL and CII. Where the schematron of the two syntaxes tests
/// a rule differently — a context that applies only when an element exists,
/// a comparison that does or does not trim — the method follows each
/// syntax's own test, so the module reports what the KoSIT validator reports.
/// </remarks>
public static partial class CoreRules
{
    /// <summary>Every core rule, by its identifier.</summary>
    public static IReadOnlyDictionary<string, Func<RuleInput, IEnumerable<string>>> All { get; } =
        new Dictionary<string, Func<RuleInput, IEnumerable<string>>>(StringComparer.Ordinal)
        {
            ["BR-01"] = Br01,
            ["BR-02"] = Br02,
            ["BR-03"] = Br03,
            ["BR-04"] = Br04,
            ["BR-05"] = Br05,
            ["BR-06"] = Br06,
            ["BR-07"] = Br07,
            ["BR-08"] = Br08,
            ["BR-09"] = Br09,
            ["BR-10"] = Br10,
            ["BR-11"] = Br11,
            ["BR-12"] = Br12,
            ["BR-13"] = Br13,
            ["BR-14"] = Br14,
            ["BR-15"] = Br15,
            ["BR-16"] = Br16,
            ["BR-17"] = Br17,
            ["BR-18"] = Br18,
            ["BR-19"] = Br19,
            ["BR-20"] = Br20,
            ["BR-21"] = Br21,
            ["BR-22"] = Br22,
            ["BR-23"] = Br23,
            ["BR-24"] = Br24,
            ["BR-25"] = Br25,
            ["BR-26"] = Br26,
            ["BR-27"] = Br27,
            ["BR-28"] = Br28,
            ["BR-29"] = Br29,
            ["BR-30"] = Br30,
            ["BR-31"] = Br31,
            ["BR-32"] = Br32,
            ["BR-33"] = Br33,
            ["BR-36"] = Br36,
            ["BR-37"] = Br37,
            ["BR-38"] = Br38,
            ["BR-41"] = Br41,
            ["BR-42"] = Br42,
            ["BR-43"] = Br43,
            ["BR-44"] = Br44,
            ["BR-45"] = Br45,
            ["BR-46"] = Br46,
            ["BR-47"] = Br47,
            ["BR-48"] = Br48,
            ["BR-49"] = Br49,
            ["BR-50"] = Br50,
            ["BR-51"] = Br51,
            ["BR-52"] = Br52,
            ["BR-53"] = Br53,
            ["BR-54"] = Br54,
            ["BR-55"] = Br55,
            ["BR-56"] = Br56,
            ["BR-57"] = Br57,
            ["BR-61"] = Br61,
            ["BR-62"] = Br62,
            ["BR-63"] = Br63,
            ["BR-64"] = Br64,
            ["BR-65"] = Br65,
        };

    /// <summary>The path, when the rule is broken; nothing when it holds.</summary>
    private static string[] BrokenAt(bool broken, string path) => broken ? [path] : [];
}
