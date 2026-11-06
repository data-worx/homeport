// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;

namespace Homeport.Platform.Registry.Domain.Manifests;

/// <summary>
/// The rules of the module contract a manifest must satisfy before the
/// registry routes to the instance that declares it. The single source of
/// these rules: a module learns about a broken manifest from the registry.
/// </summary>
public static partial class ManifestRules
{
    /// <summary>The module id no module may take: it names the platform's own endpoints.</summary>
    public const string ReservedId = "platform";

    private const string RoutesField = "routes";

    private static readonly string[] Methods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    /// <summary>
    /// Every way <paramref name="manifest"/> breaks the module contract for a
    /// registration under <paramref name="moduleId"/>, keyed by the field path
    /// in the manifest's JSON notation; empty when it holds.
    /// </summary>
    /// <param name="moduleId">The module id the registration is made for.</param>
    /// <param name="manifest">The manifest the instance declares.</param>
    public static IReadOnlyDictionary<string, string[]> Validate(string moduleId, Manifest manifest)
    {
        var errors = new Errors();

        if (!ContractVersions.Supported.Contains(manifest.ContractVersion))
        {
            errors.Add(
                "contractVersion",
                $"Contract version {manifest.ContractVersion} is not supported; supported versions: {string.Join(", ", ContractVersions.Supported)}.");
        }

        ValidateId(moduleId, manifest, errors);

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("name", "Name must not be empty.");
        }

        if (!SemanticVersions.IsValid(manifest.Version))
        {
            errors.Add("version", $"Version '{manifest.Version}' is not a semantic version.");
        }

        if (manifest.Routes.Count == 0)
        {
            errors.Add(RoutesField, "At least one route must be declared.");
        }

        for (var index = 0; index < manifest.Routes.Count; index++)
        {
            ValidateRoute(manifest, index, errors);
        }

        // A path only, so the gateway fetches the document from the instance
        // itself and from nowhere else.
        if (!OpenApiUrlPattern().IsMatch(manifest.OpenApiUrl ?? string.Empty))
        {
            errors.Add("openApiUrl", $"OpenAPI URL '{manifest.OpenApiUrl}' must be an absolute path such as /openapi/v1.json.");
        }

        return errors.ToDictionary();
    }

    private static void ValidateId(string moduleId, Manifest manifest, Errors errors)
    {
        const string field = "id";

        if (!IdPattern().IsMatch(manifest.Id))
        {
            errors.Add(field, $"Id '{manifest.Id}' must start with a lower-case letter and contain only lower-case letters, digits and '-'.");
        }

        if (manifest.Id == ReservedId)
        {
            errors.Add(field, $"Id '{ReservedId}' is reserved.");
        }

        if (manifest.Id != moduleId)
        {
            errors.Add(field, $"Id '{manifest.Id}' does not match the module id '{moduleId}' the registration is made for.");
        }
    }

    private static void ValidateRoute(Manifest manifest, int index, Errors errors)
    {
        var route = manifest.Routes[index];
        var prefix = $"{RoutesField}[{index}]";

        if (!Methods.Contains(route.Method, StringComparer.Ordinal))
        {
            errors.Add($"{prefix}.method", $"Method must be one of {string.Join(", ", Methods)}.");
        }

        if (!PathPattern().IsMatch(route.Path))
        {
            errors.Add($"{prefix}.path", $"Path '{route.Path}' must start with /v{{major}}/.");
        }

        if (!manifest.Scopes.Contains(route.Scope, StringComparer.Ordinal))
        {
            errors.Add($"{prefix}.scope", $"Scope '{route.Scope}' is not declared in the manifest's scopes.");
        }

        if (!manifest.Meters.Contains(route.Meter, StringComparer.Ordinal))
        {
            errors.Add($"{prefix}.meter", $"Meter '{route.Meter}' is not declared in the manifest's meters.");
        }

        if (route.Quantity < 1)
        {
            errors.Add($"{prefix}.quantity", "Quantity must be at least 1.");
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^/v[1-9][0-9]*/")]
    private static partial Regex PathPattern();

    // One slash, then no second one, no backslash, query or fragment: never a host.
    [GeneratedRegex(@"^/(?![/\\])[^\\?#]*$")]
    private static partial Regex OpenApiUrlPattern();

    // The messages per field, in the order they were found.
    private sealed class Errors
    {
        private readonly Dictionary<string, List<string>> messages = new(StringComparer.Ordinal);

        internal void Add(string field, string message)
        {
            if (!messages.TryGetValue(field, out var list))
            {
                list = [];
                messages[field] = list;
            }

            list.Add(message);
        }

        internal IReadOnlyDictionary<string, string[]> ToDictionary()
        {
            return messages.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
        }
    }
}
