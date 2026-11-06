// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Homeport.Platform.Gateway.Domain.ApiDocs;

/// <summary>
/// Builds the documents of the consolidated API docs (ADR-0017): one per
/// module, holding exactly the routes the gateway serves, under the paths a
/// caller uses, and one for the platform. Every document authenticates with
/// a pasted bearer token.
/// </summary>
public static partial class ConsolidatedDocuments
{
    /// <summary>The OpenAPI version every consolidated document declares.</summary>
    public const string OpenApiVersion = "3.1.1";

    /// <summary>The security scheme every operation requires: a bearer token the caller pastes.</summary>
    public const string SecuritySchemeName = "bearer";

    /// <summary>The title of the platform's document.</summary>
    public const string PlatformTitle = "Homeport platform";

    /// <summary>The server every document names: the gateway itself, as the paths are public ones.</summary>
    public const string ServerUrl = "/";

    /// <summary>The prefix a component name gets when it collides with another document's.</summary>
    public const string VersionPrefix = "v";

    private const string ComponentsReference = "#/components/";
    private const char ReferenceSeparator = '/';
    private const char NameSeparator = '_';
    private const string VersionSeparator = ", ";

    private const string OpenApiKey = "openapi";
    private const string InfoKey = "info";
    private const string TitleKey = "title";
    private const string VersionKey = "version";
    private const string ServersKey = "servers";
    private const string UrlKey = "url";
    private const string PathsKey = "paths";
    private const string ComponentsKey = "components";
    private const string SecuritySchemesKey = "securitySchemes";
    private const string SecurityKey = "security";
    private const string TagsKey = "tags";
    private const string NameKey = "name";
    private const string ReferenceKey = "$ref";
    private const string TypeKey = "type";
    private const string SchemeKey = "scheme";
    private const string BearerFormatKey = "bearerFormat";
    private const string DescriptionKey = "description";

    private const string HttpSchemeType = "http";
    private const string BearerScheme = "bearer";
    private const string JwtFormat = "JWT";
    private const string SchemeDescription = "A token from the identity provider, obtained with the client credentials of a client.";

    // The fields of a path item that hold for all its operations and are kept with them.
    private static readonly string[] PathItemFields = ["summary", "description", "parameters"];

    /// <summary>The platform's document.</summary>
    /// <param name="version">The version of Homeport the gateway runs.</param>
    public static JsonObject ForPlatform(string version)
    {
        return Skeleton(PlatformTitle, version);
    }

    /// <summary>
    /// The document of one module: for every route, the operation from the
    /// document <see cref="DocumentedRoute.OpenApiUrl"/> names, under
    /// <paramref name="publicPrefix"/>. An operation no route declares is
    /// left out, and so is a route whose document lacks it.
    /// </summary>
    /// <param name="moduleId">The module; the document's title.</param>
    /// <param name="publicPrefix">What a caller puts in front of a route's path, such as <c>/modules/echo</c>.</param>
    /// <param name="routes">The routes the gateway serves for the module.</param>
    /// <param name="sources">The documents of the module's instances, by their URL; a missing one documents nothing.</param>
    public static JsonObject ForModule(
        string moduleId,
        string publicPrefix,
        IReadOnlyList<DocumentedRoute> routes,
        IReadOnlyDictionary<Uri, JsonObject> sources)
    {
        var versions = routes.Select(route => route.Version).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var document = Skeleton(moduleId, string.Join(VersionSeparator, versions));
        var paths = document[PathsKey]!.AsObject();
        var components = new Components();
        var tags = new Tags();

        var bySource = routes
            .Where(route => sources.ContainsKey(route.OpenApiUrl))
            .GroupBy(route => route.OpenApiUrl)
            .OrderBy(source => source.Key.ToString(), StringComparer.Ordinal);

        foreach (var source in bySource)
        {
            var sourceDocument = sources[source.Key];
            var operations = source.Select(route => Find(sourceDocument, route)).OfType<Operation>().ToList();
            var renames = components.Merge(sourceDocument, source.First().Version, operations.SelectMany(operation => operation.Roots));

            foreach (var operation in operations)
            {
                Add(paths, publicPrefix + operation.PathKey, operation, renames);
                tags.Add(sourceDocument, operation.Node);
            }
        }

        components.CopyTo(document[ComponentsKey]!.AsObject());
        tags.CopyTo(document);

        return document;
    }

    private static JsonObject Skeleton(string title, string version)
    {
        return new JsonObject
        {
            [OpenApiKey] = OpenApiVersion,
            [InfoKey] = new JsonObject { [TitleKey] = title, [VersionKey] = version },
            [ServersKey] = new JsonArray(new JsonObject { [UrlKey] = ServerUrl }),
            [PathsKey] = new JsonObject(),
            [ComponentsKey] = new JsonObject
            {
                [SecuritySchemesKey] = new JsonObject
                {
                    [SecuritySchemeName] = new JsonObject
                    {
                        [TypeKey] = HttpSchemeType,
                        [SchemeKey] = BearerScheme,
                        [BearerFormatKey] = JwtFormat,
                        [DescriptionKey] = SchemeDescription,
                    },
                },
            },
            [SecurityKey] = new JsonArray(new JsonObject { [SecuritySchemeName] = new JsonArray() }),
        };
    }

    // The operation of route in source, matching the path as a route template:
    // parameter names count, constraints and case do not.
    private static Operation? Find(JsonObject source, DocumentedRoute route)
    {
        if (source[PathsKey] is not JsonObject paths)
        {
            return null;
        }

        var template = Normalize(route.Path);
        foreach (var (key, value) in paths)
        {
            if (value is JsonObject item
                && string.Equals(Normalize(key), template, StringComparison.OrdinalIgnoreCase)
                && item[route.Method.ToLowerInvariant()] is JsonObject operation)
            {
                return new Operation(key, route.Method.ToLowerInvariant(), item, operation);
            }
        }

        return null;
    }

    private static string Normalize(string path)
    {
        return Parameter().Replace(path, "{$1}").TrimEnd(ReferenceSeparator);
    }

    private static void Add(JsonObject paths, string publicPath, Operation operation, IReadOnlyDictionary<string, string> renames)
    {
        if (paths[publicPath] is not JsonObject item)
        {
            item = [];
            foreach (var field in PathItemFields)
            {
                if (operation.Item[field] is { } value)
                {
                    item[field] = Rewrite(value.DeepClone(), renames);
                }
            }

            paths[publicPath] = item;
        }

        // The document's own security applies: the gateway decides who gets through.
        var node = Rewrite(operation.Node.DeepClone(), renames).AsObject();
        node.Remove(SecurityKey);
        item[operation.Method] = node;
    }

    private static JsonNode Rewrite(JsonNode node, IReadOnlyDictionary<string, string> renames)
    {
        switch (node)
        {
            case JsonObject value:
                if (value[ReferenceKey]?.GetValueKind() == JsonValueKind.String
                    && renames.TryGetValue(value[ReferenceKey]!.GetValue<string>(), out var renamed))
                {
                    value[ReferenceKey] = renamed;
                }

                foreach (var (_, child) in value)
                {
                    if (child is not null)
                    {
                        Rewrite(child, renames);
                    }
                }

                break;
            case JsonArray items:
                foreach (var child in items)
                {
                    if (child is not null)
                    {
                        Rewrite(child, renames);
                    }
                }

                break;
        }

        return node;
    }

    private static void CollectReferences(JsonNode? node, ICollection<string> references)
    {
        switch (node)
        {
            case JsonObject value:
                if (value[ReferenceKey]?.GetValueKind() == JsonValueKind.String
                    && value[ReferenceKey]!.GetValue<string>() is var reference
                    && reference.StartsWith(ComponentsReference, StringComparison.Ordinal))
                {
                    references.Add(reference);
                }

                foreach (var (_, child) in value)
                {
                    CollectReferences(child, references);
                }

                break;
            case JsonArray items:
                foreach (var child in items)
                {
                    CollectReferences(child, references);
                }

                break;
        }
    }

    // A route template parameter with its catch-all stars, constraints, default
    // and optional marker; group 1 is its name.
    [GeneratedRegex(@"\{\*{0,2}([^}:=?]+)[^}]*\}")]
    private static partial Regex Parameter();

    // Characters OpenAPI does not allow in a component name.
    [GeneratedRegex(@"[^A-Za-z0-9.\-_]")]
    private static partial Regex NotAllowedInName();

    private sealed record Operation(string PathKey, string Method, JsonObject Item, JsonObject Node)
    {
        // Everything of the source the operation takes along.
        internal IEnumerable<JsonNode> Roots =>
            PathItemFields.Select(name => Item[name]).OfType<JsonNode>().Append(Node);
    }

    // The components every source's kept operations reference, transitively,
    // under one name each. Equal components under one name are shared; a
    // different one under a taken name is prefixed with its source's version.
    private sealed class Components
    {
        private readonly Dictionary<(string Section, string Name), JsonNode> taken = [];
        private readonly JsonObject merged = [];

        internal IReadOnlyDictionary<string, string> Merge(JsonObject source, string version, IEnumerable<JsonNode> roots)
        {
            var renames = new Dictionary<string, string>(StringComparer.Ordinal);
            var added = new List<(string Section, string Name, JsonNode Content)>();

            foreach (var reference in Closure(source, roots).Order(StringComparer.Ordinal))
            {
                if (Resolve(source, reference) is not { } resolved)
                {
                    continue;
                }

                var (section, name, content) = resolved;

                var candidate = name;
                for (var attempt = 1; taken.TryGetValue((section, candidate), out var existing) && !JsonNode.DeepEquals(existing, content); attempt++)
                {
                    var prefixed = VersionPrefix + NotAllowedInName().Replace(version, NameSeparator.ToString()) + NameSeparator + name;
                    candidate = attempt == 1 ? prefixed : $"{prefixed}{NameSeparator}{attempt}";
                }

                renames[reference] = ComponentsReference + section + ReferenceSeparator + candidate;
                if (taken.TryAdd((section, candidate), content))
                {
                    added.Add((section, candidate, content));
                }
            }

            foreach (var (section, name, content) in added)
            {
                if (merged[section] is not JsonObject target)
                {
                    target = [];
                    merged[section] = target;
                }

                target[name] = Rewrite(content.DeepClone(), renames);
            }

            return renames;
        }

        internal void CopyTo(JsonObject components)
        {
            foreach (var (section, names) in merged)
            {
                // The document's own scheme is the only one that applies.
                if (section == SecuritySchemesKey || names is not JsonObject entries)
                {
                    continue;
                }

                components[section] = entries.DeepClone();
            }
        }

        private static HashSet<string> Closure(JsonObject source, IEnumerable<JsonNode> roots)
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in roots)
            {
                CollectReferences(root, found);
            }

            var pending = new Queue<string>(found);
            while (pending.TryDequeue(out var reference))
            {
                var nested = new List<string>();
                CollectReferences(Resolve(source, reference)?.Content, nested);
                foreach (var next in nested.Where(found.Add))
                {
                    pending.Enqueue(next);
                }
            }

            return found;
        }

        private static (string Section, string Name, JsonNode Content)? Resolve(JsonObject source, string reference)
        {
            var parts = reference[ComponentsReference.Length..].Split(ReferenceSeparator, 2);
            if (parts.Length != 2 || source[ComponentsKey]?[parts[0]]?[parts[1]] is not { } content)
            {
                return null;
            }

            return (parts[0], parts[1], content);
        }
    }

    // The top-level tags the kept operations use, each taken from the first source that describes it.
    private sealed class Tags
    {
        private readonly JsonArray described = [];
        private readonly HashSet<string> names = new(StringComparer.Ordinal);

        internal void Add(JsonObject source, JsonObject operation)
        {
            if (operation[TagsKey] is not JsonArray used || source[TagsKey] is not JsonArray available)
            {
                return;
            }

            foreach (var name in used.OfType<JsonValue>().Select(tag => tag.ToString()))
            {
                var tag = available.OfType<JsonObject>().FirstOrDefault(tag => tag[NameKey]?.ToString() == name);
                if (tag is not null && names.Add(name))
                {
                    described.Add(tag.DeepClone());
                }
            }
        }

        internal void CopyTo(JsonObject document)
        {
            if (described.Count > 0)
            {
                document[TagsKey] = described.DeepClone();
            }
        }
    }
}
