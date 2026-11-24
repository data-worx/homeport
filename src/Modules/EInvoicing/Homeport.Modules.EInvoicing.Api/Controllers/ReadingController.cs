// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Sdk;
using Homeport.Shared.Client.Modules.EInvoicing.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Modules.EInvoicing.Api.Controllers;

/// <summary>Reads electronic invoices into the semantic model of EN 16931. Every action requires <see cref="EInvoicingManifest.ReadScope"/>.</summary>
[ApiController]
[Authorize(Policy = EInvoicingManifest.ReadScope)]
public sealed class ReadingController(DocumentInspection inspection) : ControllerBase
{
    // The operation id of the OpenAPI document. A client generator names its
    // method after it, so it stays stable when the action is renamed.
    private const string ReadOperation = "read";

    /// <summary>Reads the document sent as the request body into the semantic model of EN 16931.</summary>
    /// <remarks>
    /// The body is the document itself, an XML invoice or a ZUGFeRD / Factur-X
    /// PDF, declared by its content type. The response carries the report
    /// <c>validate</c> returns and the invoice, read as far as the document
    /// carries it, also when it is invalid. Amounts, quantities, prices and
    /// percentages are strings, exactly as the document writes them; dates
    /// are ISO 8601; an attached document is described, never returned. A
    /// processed document counts one in the meter <c>documents</c>; a
    /// document the module rejects counts nothing.
    /// </remarks>
    /// <param name="cancellationToken">Cancels reading the document when the caller goes away.</param>
    /// <response code="200">The document was processed; the report says what was found, the invoice what was read.</response>
    /// <response code="415">The content type is not <c>application/xml</c>, <c>text/xml</c> or <c>application/pdf</c>.</response>
    /// <response code="422">The document cannot be read as its content type declares, is a PDF without an embedded invoice, or is written in no invoice syntax or rule set the module knows.</response>
    [HttpPost(EInvoicingManifest.ReadPath, Name = ReadOperation)]
    [Consumes(MediaTypeNames.Application.Xml, MediaTypeNames.Text.Xml, MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<InvoiceReadResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<InvoiceReadResponse>> Read(CancellationToken cancellationToken)
    {
        // No request type, an exception to dotnet-core.web: the body is the
        // document as it was received, in a syntax the module does not own.
        var read = await inspection.ReadAsync(Request.Body, ReportWire.ContainerOf(Request.ContentType), cancellationToken);
        if (!read.IsSuccess)
        {
            Response.ReportUsage(EInvoicingManifest.DocumentsMeter, EInvoicingManifest.RejectedDocument);

            return EInvoicingProblems.For(read.Error).ToResult(read.Error.Message);
        }

        Response.ReportUsage(EInvoicingManifest.DocumentsMeter, EInvoicingManifest.ProcessedDocument);

        return new InvoiceReadResponse(
            ReportWire.From(read.Value.Report),
            read.Value.Invoice is { } invoice ? InvoiceWire.From(invoice) : null);
    }
}
