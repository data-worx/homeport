// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Sdk;

/// <summary>
/// Answers a request that fails model validation with
/// <see cref="HomeportProblems.Validation"/>, its errors keyed by field paths
/// in the request's JSON notation.
/// </summary>
/// <remarks>
/// MVC keys a validation error by the C# names, such as <c>Items[0].Name</c>,
/// and its <c>SystemTextJsonValidationMetadataProvider</c> does not reach the
/// parameters of a record, which every request type is. So each segment is
/// converted with the camelCase policy that ASP.NET's JSON uses by default; a
/// <c>[JsonPropertyName]</c> is not honoured.
/// </remarks>
internal static class ValidationProblems
{
    // System.Text.Json names the field of a payload it cannot read by a JSON path
    // from the root, such as "$.items[0].name"; the contract wants "items[0].name".
    private const string JsonPathRoot = "$";
    private const string JsonPathRootPrefix = "$.";
    private const char SegmentSeparator = '.';

    internal static IActionResult Create(ActionContext context)
    {
        var problem = HomeportProblems.Validation;
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var details = factory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            problem.Status,
            problem.Title,
            problem.Type.AbsoluteUri);

        var errors = details.Errors
            .GroupBy(error => FieldPath(error.Key))
            .ToDictionary(group => group.Key, group => group.SelectMany(error => error.Value).ToArray());
        details.Errors.Clear();
        foreach (var error in errors)
        {
            details.Errors.Add(error);
        }

        return new BadRequestObjectResult(details) { ContentTypes = { MediaTypeNames.Application.ProblemJson } };
    }

    private static string FieldPath(string key)
    {
        if (key == JsonPathRoot)
        {
            return string.Empty;
        }

        var path = key.StartsWith(JsonPathRootPrefix, StringComparison.Ordinal) ? key[JsonPathRootPrefix.Length..] : key;

        return string.Join(SegmentSeparator, path.Split(SegmentSeparator).Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
