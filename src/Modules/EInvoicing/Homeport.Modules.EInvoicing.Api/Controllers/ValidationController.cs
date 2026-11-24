// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Sdk;
using Homeport.Shared.Client.Modules.EInvoicing.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Modules.EInvoicing.Api.Controllers;

/// <summary>Validates electronic invoices. Every action requires <see cref="EInvoicingManifest.ValidateScope"/>.</summary>
[ApiController]
[Authorize(Policy = EInvoicingManifest.ValidateScope)]
public sealed class ValidationController(DocumentInspection inspection) : ControllerBase
{
    // The operation id of the OpenAPI document. A client generator names its
    // method after it, so it stays stable when the action is renamed.
    private const string ValidateOperation = "validate";

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
        var inspected = await inspection.InspectAsync(Request.Body, ReportWire.ContainerOf(Request.ContentType), cancellationToken);
        if (!inspected.IsSuccess)
        {
            Response.ReportUsage(EInvoicingManifest.DocumentsMeter, EInvoicingManifest.RejectedDocument);

            return EInvoicingProblems.For(inspected.Error).ToResult(inspected.Error.Message);
        }

        Response.ReportUsage(EInvoicingManifest.DocumentsMeter, EInvoicingManifest.ProcessedDocument);

        return ReportWire.From(inspected.Value);
    }
}
