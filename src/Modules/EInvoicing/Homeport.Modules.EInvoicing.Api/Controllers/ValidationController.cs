// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Sdk;
using Homeport.Shared.Client.Modules.EInvoicing.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WireContainer = Homeport.Shared.Client.Modules.EInvoicing.V1.DocumentContainer;
using WireSyntax = Homeport.Shared.Client.Modules.EInvoicing.V1.InvoiceSyntax;

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
    /// The body is the document itself, an XML invoice or a PDF, declared by
    /// its content type. The report names the container and, for XML, the
    /// syntax of the invoice. A processed document counts one in the meter
    /// <c>documents</c>; a document the module rejects counts nothing.
    /// </remarks>
    /// <param name="cancellationToken">Cancels reading the document when the caller goes away.</param>
    /// <response code="200">The document was processed; the report says what was found.</response>
    /// <response code="415">The content type is not <c>application/xml</c>, <c>text/xml</c> or <c>application/pdf</c>.</response>
    /// <response code="422">The document cannot be read as its content type declares, or is written in no invoice syntax the module knows.</response>
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

        return new ValidationReportResponse(Wire(inspected.Value.Container), Wire(inspected.Value.Syntax));
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
}
