// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Domain.RuleSets;

/// <summary>The rules that tell which rule set and version an invoice claims.</summary>
public static class RuleSetRules
{
    /// <summary>The invoice claims no specification the module knows, nor one of EN 16931.</summary>
    public const string UnknownRuleSetCode = "rule-set.unknown";

    /// <summary>The rule of the warning for an issue date outside the window of the claimed version.</summary>
    public const string WindowRule = "EINV-RULE-SET-WINDOW";

    /// <summary>The rule of the warning for an unknown specification of EN 16931, checked against EN 16931 alone.</summary>
    public const string UnknownSpecificationRule = "EINV-RULE-SET-UNKNOWN";

    /// <summary>The rule of the warning for a PDF whose XMP metadata declares another profile than its invoice claims.</summary>
    public const string ProfileRule = "EINV-XMP-PROFILE";

    /// <summary>The rule of the warning for an invoice read under a rule set that extends EN 16931, whose extension is not read.</summary>
    public const string ExtensionIgnoredRule = "EINV-EXTENSION-IGNORED";

    /// <summary>The specification identifier of EN 16931; every further specification of it starts with it.</summary>
    public const string En16931Identifier = "urn:cen.eu:en16931:2017";

    /// <summary>The rule set and version an invoice claims with its specification identifier (BT-24).</summary>
    /// <remarks>
    /// The identifier is looked up exactly. One the catalog does not list but
    /// that starts with <see cref="En16931Identifier"/> is checked against
    /// EN 16931 alone, with a warning; any other is an error. The issue date
    /// decides whether the version applies, never the day of the check: a
    /// date outside the version's window is a warning.
    /// </remarks>
    /// <param name="catalog">The rule sets the module knows.</param>
    /// <param name="customizationId">The specification identifier the invoice carries; none when it carries none.</param>
    /// <param name="issueDate">The issue date of the invoice (BT-2); none when it carries none the module can read.</param>
    public static Result<RuleSetMatch> Identify(RuleSetCatalog catalog, string? customizationId, DateOnly? issueDate)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (string.IsNullOrEmpty(customizationId))
        {
            return Result<RuleSetMatch>.Failure(new Error(
                UnknownRuleSetCode,
                "The invoice carries no specification identifier (BT-24), so the module cannot tell which rules it claims."));
        }

        if (catalog.TryGetNotAnEInvoice(customizationId, out var notAnEInvoice))
        {
            return Result<RuleSetMatch>.Success(new RuleSetMatch(customizationId, RuleSet: null, notAnEInvoice.Profile, Findings: []));
        }

        if (catalog.TryGetRuleSet(customizationId, out var version))
        {
            return Result<RuleSetMatch>.Success(new RuleSetMatch(customizationId, version, version.Profile, WindowFindings(version, issueDate)));
        }

        if (customizationId.StartsWith(En16931Identifier, StringComparison.Ordinal)
            && catalog.TryGetRuleSet(En16931Identifier, out var en16931))
        {
            Finding unknown = new(
                FindingSeverity.Warning,
                UnknownSpecificationRule,
                $"The specification '{customizationId}' is unknown to the module; the invoice is checked against EN 16931 alone.");

            return Result<RuleSetMatch>.Success(new RuleSetMatch(
                customizationId, en16931, ExpectedProfile: null, [unknown, .. WindowFindings(en16931, issueDate)]));
        }

        return Result<RuleSetMatch>.Failure(new Error(
            UnknownRuleSetCode,
            $"The specification identifier '{customizationId}' names no rule set the module knows and no specification of EN 16931."));
    }

    /// <summary>The warning for a PDF whose XMP metadata declares another profile than its invoice claims, if it does.</summary>
    /// <remarks>
    /// Spaces are ignored: the code list HybridConformanceType writes
    /// <c>EN 16931</c>, FeRD's own samples write <c>EN16931</c>, and both name
    /// the same profile. Whether the spelling is in the code list is a check
    /// of the PDF envelope (BR-HYBRID-08), which the module does not make.
    /// </remarks>
    /// <param name="match">What the invoice claims.</param>
    /// <param name="declaredProfile">The profile the XMP metadata declares (<c>ConformanceLevel</c>).</param>
    public static Finding? CheckProfile(RuleSetMatch match, string declaredProfile)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(declaredProfile);

        return match.ExpectedProfile is { } expected
            && !string.Equals(expected.Replace(" ", string.Empty, StringComparison.Ordinal), declaredProfile.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal)
            ? new Finding(
                FindingSeverity.Warning,
                ProfileRule,
                $"The XMP metadata of the PDF declares the profile '{declaredProfile}', but the invoice claims '{match.CustomizationId}', which is the profile '{expected}'.")
            : null;
    }

    /// <summary>The warning that the extension of an invoice was not read, if it claims a rule set that extends EN 16931.</summary>
    /// <remarks>
    /// XRechnung Extension and ZUGFeRD / Factur-X EXTENDED add elements to the
    /// syntax that the semantic model of EN 16931 has no term for. The model
    /// holds the core of such an invoice; the extension is left out.
    /// </remarks>
    /// <param name="match">What the invoice claims.</param>
    public static Finding? CheckExtension(RuleSetMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return match.RuleSet?.RuleSet is RuleSet.XRechnungExtension or RuleSet.FacturXExtended
            ? new Finding(
                FindingSeverity.Warning,
                ExtensionIgnoredRule,
                $"The invoice claims '{match.CustomizationId}', which extends EN 16931; the module read its EN 16931 core and ignored the extension.")
            : null;
    }

    /// <summary>The warning for an issue date outside the window of <paramref name="version"/>, if it is.</summary>
    private static Finding[] WindowFindings(RuleSetVersion version, DateOnly? issueDate) =>
        issueDate is { } date && !version.Covers(date)
            ? [new Finding(
                FindingSeverity.Warning,
                WindowRule,
                $"The invoice was issued on {date:yyyy-MM-dd}, outside the window of version {version.Version} ({Window(version)}).")]
            : [];

    /// <summary>The window of a version, in words.</summary>
    private static string Window(RuleSetVersion version) => (version.ValidFrom, version.ValidUntil) switch
    {
        ({ } from, { } until) => $"valid from {from:yyyy-MM-dd} until {until:yyyy-MM-dd}",
        ({ } from, null) => $"valid from {from:yyyy-MM-dd}",
        (null, { } until) => $"valid until {until:yyyy-MM-dd}",
        _ => "always valid",
    };
}

/// <summary>What an invoice claims with its specification identifier.</summary>
/// <param name="CustomizationId">The specification identifier, as the invoice carries it.</param>
/// <param name="RuleSet">The version of the rule set the invoice is checked against; none for a document that is no e-invoice.</param>
/// <param name="ExpectedProfile">The ZUGFeRD / Factur-X profile a PDF carrying the invoice should declare; none when no profile is known.</param>
/// <param name="Findings">What identifying the rule set found.</param>
public sealed record RuleSetMatch(
    string CustomizationId,
    RuleSetVersion? RuleSet,
    string? ExpectedProfile,
    IReadOnlyList<Finding> Findings)
{
    /// <summary>Whether the document claims a specification whose documents are no e-invoice.</summary>
    public bool IsNotAnEInvoice => RuleSet is null;
}
