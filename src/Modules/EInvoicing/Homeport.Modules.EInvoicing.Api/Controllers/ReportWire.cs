// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Shared.Client.Modules.EInvoicing.V1;
using Microsoft.Net.Http.Headers;
using WireContainer = Homeport.Shared.Client.Modules.EInvoicing.V1.DocumentContainer;
using WireRuleSet = Homeport.Shared.Client.Modules.EInvoicing.V1.RuleSet;
using WireSeverity = Homeport.Shared.Client.Modules.EInvoicing.V1.FindingSeverity;
using WireSyntax = Homeport.Shared.Client.Modules.EInvoicing.V1.InvoiceSyntax;
using WireVerdict = Homeport.Shared.Client.Modules.EInvoicing.V1.Verdict;

namespace Homeport.Modules.EInvoicing.Api.Controllers;

/// <summary>Maps what the module found about a document onto the report both actions return.</summary>
internal static class ReportWire
{
    /// <summary>The container a content type declares; <c>[Consumes]</c> has admitted only XML and PDF.</summary>
    /// <param name="contentType">The content type of the request.</param>
    public static Domain.Documents.DocumentContainer ContainerOf(string? contentType)
    {
        return MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            && parsed.MediaType.Equals(MediaTypeNames.Application.Pdf, StringComparison.OrdinalIgnoreCase)
                ? Domain.Documents.DocumentContainer.Pdf
                : Domain.Documents.DocumentContainer.Xml;
    }

    /// <summary>The report on a document.</summary>
    /// <param name="report">What the module found.</param>
    public static ValidationReportResponse From(InspectedDocument report) => new(
        Wire(report.Container),
        Wire(report.Syntax),
        report.Match?.CustomizationId,
        report.Match?.RuleSet is { } version ? Wire(version.RuleSet) : null,
        report.Match?.RuleSet?.Version,
        Wire(report.Verdict),
        [.. report.Findings.Select(Wire)]);

    /// <summary>The wire value of a container.</summary>
    private static WireContainer Wire(Domain.Documents.DocumentContainer container) => container switch
    {
        Domain.Documents.DocumentContainer.Xml => WireContainer.Xml,
        Domain.Documents.DocumentContainer.Pdf => WireContainer.Pdf,
        _ => throw new ArgumentOutOfRangeException(nameof(container), container, "Every container has a wire value."),
    };

    /// <summary>The wire value of a syntax, <see langword="null"/> for none.</summary>
    private static WireSyntax? Wire(Domain.Documents.InvoiceSyntax? syntax) => syntax switch
    {
        null => null,
        Domain.Documents.InvoiceSyntax.UblInvoice => WireSyntax.UblInvoice,
        Domain.Documents.InvoiceSyntax.UblCreditNote => WireSyntax.UblCreditNote,
        Domain.Documents.InvoiceSyntax.Cii => WireSyntax.Cii,
        _ => throw new ArgumentOutOfRangeException(nameof(syntax), syntax, "Every syntax has a wire value."),
    };

    /// <summary>The wire value of a rule set.</summary>
    private static WireRuleSet Wire(Domain.RuleSets.RuleSet ruleSet) => ruleSet switch
    {
        Domain.RuleSets.RuleSet.En16931 => WireRuleSet.En16931,
        Domain.RuleSets.RuleSet.XRechnung => WireRuleSet.XRechnung,
        Domain.RuleSets.RuleSet.XRechnungExtension => WireRuleSet.XRechnungExtension,
        Domain.RuleSets.RuleSet.FacturXBasic => WireRuleSet.FacturXBasic,
        Domain.RuleSets.RuleSet.FacturXExtended => WireRuleSet.FacturXExtended,
        _ => throw new ArgumentOutOfRangeException(nameof(ruleSet), ruleSet, "Every rule set has a wire value."),
    };

    /// <summary>The wire value of a verdict.</summary>
    private static WireVerdict Wire(Domain.Documents.Verdict verdict) => verdict switch
    {
        Domain.Documents.Verdict.Valid => WireVerdict.Valid,
        Domain.Documents.Verdict.Invalid => WireVerdict.Invalid,
        Domain.Documents.Verdict.NotAnEInvoice => WireVerdict.NotAnEInvoice,
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Every verdict has a wire value."),
    };

    /// <summary>The wire form of a finding.</summary>
    private static ValidationFinding Wire(Domain.Findings.Finding finding) => new(
        finding.Severity switch
        {
            Domain.Findings.FindingSeverity.Fatal => WireSeverity.Fatal,
            Domain.Findings.FindingSeverity.Warning => WireSeverity.Warning,
            Domain.Findings.FindingSeverity.Information => WireSeverity.Information,
            _ => throw new ArgumentOutOfRangeException(nameof(finding), finding.Severity, "Every severity has a wire value."),
        },
        finding.Rule,
        finding.Message,
        finding.Location,
        finding.Path,
        finding.BusinessTerm);
}
