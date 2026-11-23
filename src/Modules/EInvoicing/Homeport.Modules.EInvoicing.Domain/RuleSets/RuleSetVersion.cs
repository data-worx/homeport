// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.RuleSets;

/// <summary>One version of a rule set, as the rule-set catalog lists it.</summary>
/// <param name="CustomizationId">The specification identifier (BT-24) an invoice of this version carries.</param>
/// <param name="RuleSet">The rule set.</param>
/// <param name="Version">The version the specification identifier names.</param>
/// <param name="ValidFrom">The first issue date the version is valid for, as its source states; none when the source states none.</param>
/// <param name="ValidUntil">The last issue date the version is valid for, as its source states; none when the source states none.</param>
/// <param name="Profile">The ZUGFeRD / Factur-X profile a PDF carrying this version declares in its XMP metadata; none for a version no profile carries.</param>
/// <param name="Standard">The name of the standard in the module's <c>standards.json</c> the version is taken from.</param>
public sealed record RuleSetVersion(
    string CustomizationId,
    RuleSet RuleSet,
    string Version,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    string? Profile,
    string Standard)
{
    /// <summary>Whether an invoice issued on <paramref name="issueDate"/> lies inside the version's window.</summary>
    /// <param name="issueDate">The issue date of the invoice (BT-2).</param>
    public bool Covers(DateOnly issueDate) =>
        (ValidFrom is not { } from || issueDate >= from) && (ValidUntil is not { } until || issueDate <= until);
}
