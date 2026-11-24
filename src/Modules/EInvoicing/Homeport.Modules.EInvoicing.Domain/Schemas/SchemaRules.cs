// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Homeport.Modules.EInvoicing.Domain.Findings;

namespace Homeport.Modules.EInvoicing.Domain.Schemas;

/// <summary>The first stage of validation: an invoice against the XML schema of its syntax.</summary>
public static class SchemaRules
{
    /// <summary>The rule of every fatal finding against the XML schema.</summary>
    public const string SchemaRule = "EINV-SCHEMA";

    /// <summary>The rule of the information that findings beyond <see cref="MaxFindings"/> were not reported.</summary>
    public const string LimitRule = "EINV-SCHEMA-LIMIT";

    /// <summary>The most findings against the schema one document gets; a document broken throughout would get thousands.</summary>
    public const int MaxFindings = 100;

    /// <summary>The findings of <paramref name="root"/> against the schemas of <paramref name="catalog"/>; empty when it is valid.</summary>
    /// <remarks>
    /// The invoice is walked node by node through an <see cref="XmlSchemaValidator"/>
    /// rather than validated in one call, so a finding can name what the
    /// schema expected at that point — a missing mandatory element above
    /// all — in a message of the module's own, independent of the wording
    /// of the framework. Neither <c>xsi:schemaLocation</c> nor inline schemas
    /// are followed: only the embedded schemas apply. Each finding carries
    /// the location of the node in the document.
    /// </remarks>
    /// <param name="root">The root element of the invoice.</param>
    /// <param name="catalog">The compiled schemas.</param>
    public static IReadOnlyList<Finding> Validate(XElement root, SchemaCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(catalog);

        var walk = new SchemaWalk(catalog.Schemas);
        walk.Run(root);

        return walk.Findings;
    }

    /// <summary>One walk of a document through the validator, collecting what it finds.</summary>
    private sealed class SchemaWalk
    {
        // The root of the built-in simple types, which says nothing about a value.
        private const string AnySimpleType = "anySimpleType";

        private readonly XmlSchemaValidator validator;
        private readonly ElementNamespaces namespaces = new();
        private readonly List<Finding> findings = [];

        // What the validator is checking while it may report an error: set
        // before every call, so the handler knows which message to write.
        private Func<Finding>? pending;
        private bool reported;
        private bool limitReached;

        /// <summary>Prepares a walk against <paramref name="schemas"/>.</summary>
        /// <param name="schemas">The compiled schemas.</param>
        public SchemaWalk(XmlSchemaSet schemas)
        {
            // No flag set: no xsi:schemaLocation, no inline schema, no
            // identity constraints, and no warnings for content a wildcard
            // admits without a schema, such as a UBL extension's.
            validator = new XmlSchemaValidator(new NameTable(), schemas, namespaces, XmlSchemaValidationFlags.None);
            validator.ValidationEventHandler += OnValidationEvent;
        }

        /// <summary>What the walk found.</summary>
        public IReadOnlyList<Finding> Findings => findings;

        /// <summary>Walks the document from its root element.</summary>
        /// <param name="root">The root element.</param>
        public void Run(XElement root)
        {
            validator.Initialize();
            Element(root);
            if (!limitReached)
            {
                validator.EndValidation();
            }
        }

        /// <summary>Validates an element, its attributes and its content.</summary>
        private void Element(XElement element)
        {
            if (limitReached)
            {
                return;
            }

            namespaces.Current = element;
            var expected = validator.GetExpectedParticles();

            // Filled by the validator with the element's type, which tells at
            // its end whether a failure concerns its value or its children.
            var elementInfo = new XmlSchemaInfo();
            Check(() => ElementFinding(element, expected), () => validator.ValidateElement(element.Name.LocalName, element.Name.NamespaceName, elementInfo));

            foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
            {
                var declared = validator.GetExpectedAttributes().Any(candidate => candidate.QualifiedName == ToQualifiedName(attribute.Name));
                Check(
                    () => AttributeFinding(attribute, declared),
                    () => validator.ValidateAttribute(attribute.Name.LocalName, attribute.Name.NamespaceName, attribute.Value, schemaInfo: null));
            }

            var missing = validator.GetExpectedAttributes();
            Check(() => MissingAttributeFinding(element, missing), () => validator.ValidateEndOfAttributes(schemaInfo: null));

            foreach (var node in element.Nodes())
            {
                switch (node)
                {
                    case XElement child:
                        Element(child);
                        namespaces.Current = element;
                        break;
                    case XText text when string.IsNullOrWhiteSpace(text.Value):
                        validator.ValidateWhitespace(text.Value);
                        break;
                    case XText text:
                        Check(() => TextFinding(element), () => validator.ValidateText(text.Value));
                        break;
                }
            }

            if (limitReached)
            {
                return;
            }

            var remaining = validator.GetExpectedParticles();
            Check(() => EndFinding(element, elementInfo, remaining), () => validator.ValidateEndElement(schemaInfo: null));
        }

        /// <summary>Makes one call to the validator, reporting at most one finding for it.</summary>
        private void Check(Func<Finding> finding, Action call)
        {
            pending = finding;
            reported = false;
            call();
            pending = null;
        }

        /// <summary>Records a finding for the error the validator reports, written by the call that caused it.</summary>
        private void OnValidationEvent(object? sender, ValidationEventArgs args)
        {
            if (args.Severity != XmlSeverityType.Error || reported || limitReached || pending is null)
            {
                return;
            }

            reported = true;
            if (findings.Count == MaxFindings)
            {
                limitReached = true;
                findings.Add(new Finding(
                    FindingSeverity.Information,
                    LimitRule,
                    $"The document breaks its XML schema in more than {MaxFindings} places; only the first {MaxFindings} are reported."));

                return;
            }

            findings.Add(pending());
        }

        /// <summary>The finding for an element the schema does not admit where it stands.</summary>
        private static Finding ElementFinding(XElement element, XmlSchemaParticle[] expected)
        {
            var name = NameOf(element);
            var mandatory = Mandatory(expected);
            var message = mandatory.Count > 0
                ? $"The element '{name}' stands where the mandatory element {Names(element, mandatory)} is expected."
                : expected.Length > 0
                    ? $"The element '{name}' is not allowed here; expected {Names(element, expected)}."
                    : $"The element '{name}' is not allowed here; no further element is expected.";

            return Fatal(message, LocationOf(element));
        }

        /// <summary>The finding for an attribute the schema does not declare, or whose value does not fit its type.</summary>
        private static Finding AttributeFinding(XAttribute attribute, bool declared)
        {
            var name = NameOf(attribute);
            var owner = NameOf(attribute.Parent!);
            var message = declared
                ? $"The value of the attribute '{name}' of '{owner}' is not valid for its type."
                : $"The element '{owner}' has no attribute '{name}'.";

            return Fatal(message, $"{LocationOf(attribute.Parent!)}/@{name}");
        }

        /// <summary>The finding for an element that lacks mandatory attributes, naming them.</summary>
        private static Finding MissingAttributeFinding(XElement element, XmlSchemaAttribute[] candidates)
        {
            var required = candidates
                .Where(attribute => attribute.Use == XmlSchemaUse.Required)
                .Select(attribute => $"'{NameOf(element, attribute.QualifiedName)}'")
                .ToList();
            var message = required.Count > 0
                ? $"The element '{NameOf(element)}' lacks the mandatory attribute {string.Join(" or ", required)}."
                : $"The element '{NameOf(element)}' lacks a mandatory attribute.";

            return Fatal(message, LocationOf(element));
        }

        /// <summary>The finding for text where the schema admits none.</summary>
        private static Finding TextFinding(XElement element) =>
            Fatal($"The element '{NameOf(element)}' does not take text.", LocationOf(element));

        /// <summary>The finding for an element whose content ends incomplete, or whose value does not fit its type.</summary>
        private static Finding EndFinding(XElement element, XmlSchemaInfo info, XmlSchemaParticle[] remaining)
        {
            var name = NameOf(element);
            if (info.ContentType == XmlSchemaContentType.TextOnly)
            {
                var type = TypeNameOf(info.SchemaType);
                var message = type is null
                    ? $"The value of '{name}' is not valid for its type."
                    : $"The value of '{name}' is not a valid {type}.";

                return Fatal(message, LocationOf(element));
            }

            var mandatory = Mandatory(remaining);
            var incomplete = mandatory.Count > 0
                ? $"The element '{name}' lacks the mandatory element {Names(element, mandatory)}."
                : $"The element '{name}' is not complete.";

            return Fatal(incomplete, LocationOf(element));
        }

        /// <summary>A fatal finding against the schema.</summary>
        private static Finding Fatal(string message, string location) => new(FindingSeverity.Fatal, SchemaRule, message, location);

        /// <summary>The expected particles of which at least one must follow.</summary>
        private static List<XmlSchemaParticle> Mandatory(XmlSchemaParticle[] expected) =>
            [.. expected.Where(particle => particle.MinOccurs > 0)];

        /// <summary>The names of expected particles, joined with <c>or</c>.</summary>
        private static string Names(XElement context, IEnumerable<XmlSchemaParticle> particles) =>
            string.Join(" or ", particles.Select(particle => particle switch
            {
                XmlSchemaElement declared => $"'{NameOf(context, declared.QualifiedName)}'",
                XmlSchemaAny => "an element of another namespace",
                _ => "further content",
            }).Distinct());

        /// <summary>The built-in XML Schema type a schema type derives from, such as <c>date</c>; none when it derives from none.</summary>
        /// <remarks>
        /// UBL and CII wrap every value in types of their own; the built-in
        /// type at the bottom is what the value has to fit.
        /// </remarks>
        private static string? TypeNameOf(XmlSchemaType? type)
        {
            for (var current = type; current is not null; current = current.BaseXmlSchemaType)
            {
                if (current.QualifiedName.Namespace == XmlSchema.Namespace && current.QualifiedName.Name != AnySimpleType)
                {
                    return current.QualifiedName.Name;
                }
            }

            return null;
        }

        /// <summary>The name of an element as the document writes it.</summary>
        private static string NameOf(XElement element) => NameOf(element, ToQualifiedName(element.Name));

        /// <summary>The name of an attribute as the document writes it.</summary>
        private static string NameOf(XAttribute attribute) =>
            attribute.Name.Namespace == XNamespace.None
                ? attribute.Name.LocalName
                : NameOf(attribute.Parent!, ToQualifiedName(attribute.Name));

        /// <summary>A name with the prefix the document binds to its namespace at <paramref name="context"/>.</summary>
        private static string NameOf(XElement context, XmlQualifiedName name)
        {
            if (string.IsNullOrEmpty(name.Namespace))
            {
                return name.Name;
            }

            var prefix = context.GetPrefixOfNamespace(name.Namespace);
            if (prefix is not null)
            {
                return $"{prefix}:{name.Name}";
            }

            return context.GetDefaultNamespace().NamespaceName == name.Namespace
                ? name.Name
                : $"{{{name.Namespace}}}{name.Name}";
        }

        /// <summary>The location of an element: its path from the root, with the position among its namesakes.</summary>
        private static string LocationOf(XElement element)
        {
            var steps = element.AncestorsAndSelf().Reverse().Select(step => step.Parent is null
                ? $"/{NameOf(step)}"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"/{NameOf(step)}[{step.ElementsBeforeSelf(step.Name).Count() + 1}]"));

            return string.Concat(steps);
        }

        /// <summary>The schema's form of a name.</summary>
        private static XmlQualifiedName ToQualifiedName(XName name) => new(name.LocalName, name.NamespaceName);
    }

    /// <summary>The namespaces in scope at the element the walk stands on, for values that are qualified names.</summary>
    private sealed class ElementNamespaces : IXmlNamespaceResolver
    {
        /// <summary>The element the walk stands on.</summary>
        public XElement? Current { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope) =>
            Current?.AncestorsAndSelf()
                .SelectMany(element => element.Attributes())
                .Where(attribute => attribute.IsNamespaceDeclaration)
                .GroupBy(attribute => attribute.Name.Namespace == XNamespace.None ? string.Empty : attribute.Name.LocalName)
                .ToDictionary(group => group.Key, group => group.First().Value)
            ?? [];

        /// <inheritdoc/>
        public string? LookupNamespace(string prefix) => prefix.Length == 0
            ? Current?.GetDefaultNamespace().NamespaceName
            : Current?.GetNamespaceOfPrefix(prefix)?.NamespaceName;

        /// <inheritdoc/>
        public string? LookupPrefix(string namespaceName) => Current?.GetPrefixOfNamespace(namespaceName);
    }
}
