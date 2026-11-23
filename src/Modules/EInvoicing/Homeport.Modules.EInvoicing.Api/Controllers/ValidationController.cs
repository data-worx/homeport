// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Sdk;
using Homeport.Shared.Client.Modules.EInvoicing.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WireContainer = Homeport.Shared.Client.Modules.EInvoicing.V1.DocumentContainer;
using WireRuleSet = Homeport.Shared.Client.Modules.EInvoicing.V1.RuleSet;
using WireSeverity = Homeport.Shared.Client.Modules.EInvoicing.V1.FindingSeverity;
using WireSyntax = Homeport.Shared.Client.Modules.EInvoicing.V1.InvoiceSyntax;
using WireVerdict = Homeport.Shared.Client.Modules.EInvoicing.V1.Verdict;

namespace Homeport.Modules.EInvoicing.Api.Controllers;

/// <summary>Validates electronic invoices. Every action requires <see cref="EInvoicingManifest.ValidateScope"/>.</summary>
[ApiController]
[Authorize(Policy = EInvoicingManifest.ValidateScope)]
public sealed class ValidationController(DocumentInspection inspection) : ControllerBase
{
    // The operation id of the OpenAPI document. A client generator names its
    // method after it, so it stays stable when the action is renamed.
    private const string ValidateOperation = "validate";

    // The one document a processed request counts in the meter documents.
    private const int ProcessedDocument = 1;

    /// <summary>Validates the document sent as the request body.</summary>
    /// <remarks>
    /// The body is the document itself, an XML invoice or a ZUGFeRD / Factur-X
    /// PDF, declared by its content type. The report names the container, the
    /// syntax, the specification identifier and the rule set and version it
    /// claims, a verdict for a document that is no e-invoice, and warnings. A
    /// processed document counts one in the meter <c>documents</c>; a document
    /// the module rejects counts nothing.
    /// </remarks>
    /// <param name="cancellationToken">Cancels reading the document when the caller goes away.</param>
    /// <response code="200">The document was processed; the report says what was found.</response>
    /// <response code="415">The content type is not <c>application/xml</c>, <c>text/xml</c> or <c>application/pdf</c>.</response>
    /// <response code="422">The document cannot be read as its content type declares, is a PDF without an embedded invoice, or is written in no invoice syntax or rule set the module knows.</response>
    [HttpPost(EInvoicingManifest.ValidatePath, Name = ValidateOperation)]
    [Consumes(MediaTypeNames.Application.Xml, MediaTypeNames.Text.Xml, MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<ValidationReportResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ValidationReportResponse>> Validate(CancellationToken cancellationToken)
    {
        // No request type, an exception to dotnet-core.web: the body is the
        // document as it was received, in a syntax the module does not own.
        var inspected = await inspection.InspectAsync(Request.Body, ContainerOf(Request.ContentType), cancellationToken);
        if (!inspected.IsSuccess)
        {
            Response.ReportUsage(EInvoicingManifest.DocumentsMeter, 0);

            return EInvoicingProblems.For(inspected.Error).ToResult(inspected.Error.Message);
        }

        Response.ReportUsage(EInvoicingManifest.DocumentsMeter, ProcessedDocument);

        var report = inspected.Value;

        return new ValidationReportResponse(
            Wire(report.Container),
            Wire(report.Syntax),
            report.Match?.CustomizationId,
            report.Match?.RuleSet is { } version ? Wire(version.RuleSet) : null,
            report.Match?.RuleSet?.Version,
            report.Verdict is { } verdict ? Wire(verdict) : null,
            [.. report.Findings.Select(Wire)]);
    }

    /// <summary>The container a content type declares; <c>[Consumes]</c> has admitted only XML and PDF.</summary>
    private static Domain.Documents.DocumentContainer ContainerOf(string? contentType)
    {
        return MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            && parsed.MediaType.Equals(MediaTypeNames.Application.Pdf, StringComparison.OrdinalIgnoreCase)
                ? Domain.Documents.DocumentContainer.Pdf
                : Domain.Documents.DocumentContainer.Xml;
    }

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
        Domain.Documents.Verdict.NotAnEInvoice => WireVerdict.NotAnEInvoice,
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Every verdict has a wire value."),
    };

    /// <summary>The wire form of a finding.</summary>
    private static ValidationFinding Wire(Domain.Findings.Finding finding) => new(
        finding.Severity switch
        {
            Domain.Findings.FindingSeverity.Warning => WireSeverity.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(finding), finding.Severity, "Every severity has a wire value."),
        },
        finding.Rule,
        finding.Message);
}
