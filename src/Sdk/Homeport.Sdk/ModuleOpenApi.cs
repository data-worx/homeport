// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Shared.Client.ModuleContract;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>
/// Checks a module's generated OpenAPI document for what a caller needs from
/// every operation: a <c>summary</c>, an <c>operationId</c> and a success
/// response. A module runs it in a test against the document it serves at
/// <see cref="ModuleManifest.OpenApiPath"/>, so an undocumented endpoint
/// fails the build rather than reaching a caller.
/// </summary>
public static class ModuleOpenApi
{
    private const string PathsProperty = "paths";
    private const string SummaryProperty = "summary";
    private const string OperationIdProperty = "operationId";
    private const string ResponsesProperty = "responses";

    // A success status: 2xx, or the OpenAPI wildcard 2XX.
    private const char SuccessClass = '2';

    // The keys of a path item that name an operation; the others, such as
    // "parameters", describe the path.
    private static readonly string[] Methods =
    [
        HttpMethods.Get, HttpMethods.Put, HttpMethods.Post, HttpMethods.Delete,
        HttpMethods.Options, HttpMethods.Head, HttpMethods.Patch, HttpMethods.Trace,
    ];

    /// <summary>Returns every problem found in <paramref name="document"/>; empty when there is none.</summary>
    /// <param name="document">The root of the OpenAPI document as JSON.</param>
    public static IReadOnlyList<string> Validate(JsonElement document)
    {
        if (!document.TryGetProperty(PathsProperty, out var paths) || paths.ValueKind != JsonValueKind.Object)
        {
            return ["The document has no paths."];
        }

        var problems = new List<string>();
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var item in path.Value.EnumerateObject())
            {
                var method = Array.Find(Methods, known => string.Equals(known, item.Name, StringComparison.OrdinalIgnoreCase));
                if (method is not null)
                {
                    problems.AddRange(ValidateOperation($"{method} {path.Name}", item.Value));
                }
            }
        }

        return problems;
    }

    private static IEnumerable<string> ValidateOperation(string operation, JsonElement element)
    {
        if (!HasText(element, SummaryProperty))
        {
            yield return $"{operation} has no summary.";
        }

        if (!HasText(element, OperationIdProperty))
        {
            yield return $"{operation} has no operationId.";
        }

        var hasSuccess = element.TryGetProperty(ResponsesProperty, out var responses)
            && responses.ValueKind == JsonValueKind.Object
            && responses.EnumerateObject().Any(response => response.Name.StartsWith(SuccessClass));
        if (!hasSuccess)
        {
            yield return $"{operation} has no success response.";
        }
    }

    private static bool HasText(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString());
    }
}
