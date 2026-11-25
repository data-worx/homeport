// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeport.Modules.EInvoicing.Domain.RuleSets;

/// <summary>
/// The rule sets the module knows, and the specifications it knows to be no
/// e-invoice, each by its specification identifier (BT-24).
/// </summary>
/// <remarks>
/// The catalog is data embedded in the module, <c>rule-sets.json</c>, and is
/// versioned with it; an operator cannot override it. Every window in it is
/// taken from the source of its version, never estimated.
/// </remarks>
public sealed class RuleSetCatalog
{
    private const string ResourceName = "Homeport.Modules.EInvoicing.Domain.RuleSets.rule-sets.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    // Spelled out rather than left to a naming policy, which would turn
    // XRechnung into x-rechnung.
    private static readonly Dictionary<string, RuleSet> RuleSetNames = new(StringComparer.Ordinal)
    {
        ["en16931"] = RuleSet.En16931,
        ["xrechnung"] = RuleSet.XRechnung,
        ["xrechnung-extension"] = RuleSet.XRechnungExtension,
        ["factur-x-basic"] = RuleSet.FacturXBasic,
        ["factur-x-extended"] = RuleSet.FacturXExtended,
    };

    private readonly Dictionary<string, RuleSetVersion> ruleSets;
    private readonly Dictionary<string, NotAnEInvoiceSpecification> notAnEInvoice;

    /// <summary>Creates a catalog of the given entries; every specification identifier occurs once.</summary>
    /// <param name="ruleSets">The versions of the rule sets.</param>
    /// <param name="notAnEInvoice">The specifications whose documents are no e-invoice.</param>
    /// <exception cref="ArgumentException">A specification identifier occurs twice.</exception>
    public RuleSetCatalog(IEnumerable<RuleSetVersion> ruleSets, IEnumerable<NotAnEInvoiceSpecification> notAnEInvoice)
    {
        ArgumentNullException.ThrowIfNull(ruleSets);
        ArgumentNullException.ThrowIfNull(notAnEInvoice);

        this.ruleSets = ruleSets.ToDictionary(version => version.CustomizationId, StringComparer.Ordinal);
        this.notAnEInvoice = notAnEInvoice.ToDictionary(specification => specification.CustomizationId, StringComparer.Ordinal);

        var both = this.ruleSets.Keys.Intersect(this.notAnEInvoice.Keys, StringComparer.Ordinal).FirstOrDefault();
        if (both is not null)
        {
            throw new ArgumentException($"The specification identifier '{both}' is both a rule set and no e-invoice.", nameof(notAnEInvoice));
        }
    }

    /// <summary>Every version of every rule set.</summary>
    public IReadOnlyCollection<RuleSetVersion> RuleSets => ruleSets.Values;

    /// <summary>Every specification whose documents are no e-invoice.</summary>
    public IReadOnlyCollection<NotAnEInvoiceSpecification> NotAnEInvoice => notAnEInvoice.Values;

    /// <summary>The catalog embedded in the module.</summary>
    /// <exception cref="InvalidOperationException">The embedded catalog is missing or empty, a defect of the build.</exception>
    public static RuleSetCatalog LoadEmbedded()
    {
        using var stream = typeof(RuleSetCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The module carries no embedded resource '{ResourceName}'.");
        var file = JsonSerializer.Deserialize<CatalogFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("The embedded rule-set catalog is empty.");

        return new RuleSetCatalog(
            file.RuleSets.Select(entry => new RuleSetVersion(
                entry.CustomizationId, RuleSetNamed(entry.RuleSet), entry.Version, entry.ValidFrom, entry.ValidUntil, entry.Profile, entry.Standard)),
            file.NotAnEInvoice.Select(entry => new NotAnEInvoiceSpecification(entry.CustomizationId, entry.Profile, entry.Standard)));
    }

    /// <summary>The version of a rule set an invoice with <paramref name="customizationId"/> claims, if the catalog lists one.</summary>
    /// <param name="customizationId">The specification identifier, compared exactly.</param>
    /// <param name="version">The version, when there is one.</param>
    public bool TryGetRuleSet(string customizationId, [NotNullWhen(true)] out RuleSetVersion? version) =>
        ruleSets.TryGetValue(customizationId, out version);

    /// <summary>The specification a document with <paramref name="customizationId"/> claims, if the catalog lists it as no e-invoice.</summary>
    /// <param name="customizationId">The specification identifier, compared exactly.</param>
    /// <param name="specification">The specification, when there is one.</param>
    public bool TryGetNotAnEInvoice(string customizationId, [NotNullWhen(true)] out NotAnEInvoiceSpecification? specification) =>
        notAnEInvoice.TryGetValue(customizationId, out specification);

    /// <summary>The rule set a name in the embedded data stands for, such as <c>xrechnung-extension</c>, if it names one.</summary>
    /// <param name="name">The name, compared exactly.</param>
    /// <param name="ruleSet">The rule set, when the name names one.</param>
    internal static bool TryParseRuleSet(string name, out RuleSet ruleSet) => RuleSetNames.TryGetValue(name, out ruleSet);

    /// <summary>The rule set a name in <c>rule-sets.json</c> stands for.</summary>
    private static RuleSet RuleSetNamed(string name) => RuleSetNames.TryGetValue(name, out var ruleSet)
        ? ruleSet
        : throw new InvalidOperationException($"The embedded rule-set catalog names the unknown rule set '{name}'.");

    /// <summary>The shape of <c>rule-sets.json</c>.</summary>
    /// <param name="RuleSets">The versions of the rule sets.</param>
    /// <param name="NotAnEInvoice">The specifications of no e-invoice.</param>
    private sealed record CatalogFile(IReadOnlyList<RuleSetEntry> RuleSets, IReadOnlyList<NotAnEInvoiceEntry> NotAnEInvoice);

    /// <summary>A version of a rule set in <c>rule-sets.json</c>; the members are those of <see cref="RuleSetVersion"/>.</summary>
    /// <param name="CustomizationId">See <see cref="RuleSetVersion.CustomizationId"/>.</param>
    /// <param name="RuleSet">The name of <see cref="RuleSetVersion.RuleSet"/>, as <see cref="RuleSetNames"/> lists it.</param>
    /// <param name="Version">See <see cref="RuleSetVersion.Version"/>.</param>
    /// <param name="ValidFrom">See <see cref="RuleSetVersion.ValidFrom"/>.</param>
    /// <param name="ValidUntil">See <see cref="RuleSetVersion.ValidUntil"/>.</param>
    /// <param name="Profile">See <see cref="RuleSetVersion.Profile"/>.</param>
    /// <param name="Standard">See <see cref="RuleSetVersion.Standard"/>.</param>
    private sealed record RuleSetEntry(
        string CustomizationId,
        string RuleSet,
        string Version,
        DateOnly? ValidFrom,
        DateOnly? ValidUntil,
        string? Profile,
        string Standard);

    /// <summary>A specification of no e-invoice in <c>rule-sets.json</c>; the members are those of <see cref="NotAnEInvoiceSpecification"/>.</summary>
    /// <param name="CustomizationId">See <see cref="NotAnEInvoiceSpecification.CustomizationId"/>.</param>
    /// <param name="Profile">See <see cref="NotAnEInvoiceSpecification.Profile"/>.</param>
    /// <param name="Standard">See <see cref="NotAnEInvoiceSpecification.Standard"/>.</param>
    private sealed record NotAnEInvoiceEntry(string CustomizationId, string Profile, string Standard);
}
