// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Homeport.Sdk;

/// <summary>
/// Adds the errors every module operation can answer with to its OpenAPI
/// operation, as Problem Details: the gateway's <c>401</c> and <c>429</c>,
/// the module's <c>403</c> for a missing scope, and <c>500</c>.
/// </summary>
/// <remarks>
/// A status the action declares itself is left as it is. Every response points
/// at the one <c>ProblemDetails</c> schema in the document's components.
/// </remarks>
internal sealed class CommonErrorResponses : IOpenApiOperationTransformer
{
    private const string ProblemDetailsSchemaId = nameof(ProblemDetails);

    private static readonly IReadOnlyList<(int Status, string Description)> Errors =
    [
        (StatusCodes.Status401Unauthorized, "The request carries no valid token."),
        (StatusCodes.Status403Forbidden, "The token lacks the scope the operation requires."),
        (StatusCodes.Status429TooManyRequests, "A rate limit or a quota of the tenant is exhausted."),
        (StatusCodes.Status500InternalServerError, "The request failed for a reason the caller cannot influence."),
    ];

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        operation.Responses ??= [];
        var schema = await ProblemDetailsSchemaAsync(context, cancellationToken);

        foreach (var (status, description) in Errors)
        {
            var key = status.ToString(CultureInfo.InvariantCulture);
            if (operation.Responses.ContainsKey(key))
            {
                continue;
            }

            operation.Responses[key] = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.ProblemJson] = new() { Schema = schema },
                },
            };
        }
    }

    private static async Task<IOpenApiSchema> ProblemDetailsSchemaAsync(
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var document = context.Document
            ?? throw new InvalidOperationException("An operation is transformed without its document.");

        if (document.Components?.Schemas?.ContainsKey(ProblemDetailsSchemaId) != true)
        {
            var schema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
            document.AddComponent(ProblemDetailsSchemaId, schema);
        }

        return new OpenApiSchemaReference(ProblemDetailsSchemaId, document);
    }
}
