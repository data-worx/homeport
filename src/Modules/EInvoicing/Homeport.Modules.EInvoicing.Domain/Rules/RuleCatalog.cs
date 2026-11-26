// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The business rules the module checks an e-invoice against, with what it reports for each.</summary>
/// <remarks>
/// The catalog is data embedded in the module, <c>rules.json</c>, and is
/// versioned with it; an operator cannot override it. The rule identifiers
/// are the official ones; the messages are the module's own.
/// </remarks>
public sealed class RuleCatalog
{
    private const string ResourceName = "Homeport.Modules.EInvoicing.Domain.Rules.rules.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    // Spelled out, like the rule-set names, rather than left to a naming policy.
    private static readonly Dictionary<string, FindingSeverity> SeverityNames = new(StringComparer.Ordinal)
    {
        ["fatal"] = FindingSeverity.Fatal,
        ["warning"] = FindingSeverity.Warning,
        ["information"] = FindingSeverity.Information,
    };

    // The syntaxes an override names, spelled as the wire form of the report spells them.
    private static readonly Dictionary<string, InvoiceSyntax> SyntaxNames = new(StringComparer.Ordinal)
    {
        ["ubl-invoice"] = InvoiceSyntax.UblInvoice,
        ["ubl-credit-note"] = InvoiceSyntax.UblCreditNote,
        ["cii"] = InvoiceSyntax.Cii,
    };

    /// <summary>Creates a catalog of the given rules; every identifier occurs once.</summary>
    /// <param name="rules">The rules, in the order they are checked and reported.</param>
    /// <exception cref="ArgumentException">An identifier occurs twice.</exception>
    public RuleCatalog(IEnumerable<RuleDefinition> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        Rules = [.. rules];
        var twice = Rules.GroupBy(rule => rule.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (twice is not null)
        {
            throw new ArgumentException($"The rule '{twice.Key}' occurs twice.", nameof(rules));
        }
    }

    /// <summary>Every rule, in the order they are checked and reported.</summary>
    public IReadOnlyList<RuleDefinition> Rules { get; }

    /// <summary>The catalog embedded in the module.</summary>
    /// <exception cref="InvalidOperationException">The embedded catalog is missing, empty or names something unknown, a defect of the build.</exception>
    public static RuleCatalog LoadEmbedded()
    {
        using var stream = typeof(RuleCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The module carries no embedded resource '{ResourceName}'.");
        var file = JsonSerializer.Deserialize<CatalogFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("The embedded rule catalog is empty.");

        return new RuleCatalog(file.Groups.SelectMany(group =>
        {
            var ruleSets = group.RuleSets.Select(RuleSetNamed).ToHashSet();

            return group.Rules.Select(rule => new RuleDefinition(
                rule.Id,
                group.Name,
                group.Standard,
                rule.BusinessTerm,
                SeverityNamed(rule.Severity),
                rule.SeverityInUbl is { } inUbl ? SeverityNamed(inUbl) : null,
                rule.SeverityInCii is { } inCii ? SeverityNamed(inCii) : null,
                rule.Message,
                ruleSets)
            {
                Overrides = [.. (rule.Overrides ?? []).Select(entry => OverrideOf(rule.Id, entry, ruleSets))],
            });
        }));
    }

    /// <summary>The override an entry in <c>rules.json</c> stands for, under a rule set the rule is checked under.</summary>
    private static SeverityOverride OverrideOf(string ruleId, OverrideEntry entry, IReadOnlySet<RuleSet> ruleSets)
    {
        var ruleSet = RuleSetNamed(entry.RuleSet);
        if (!ruleSets.Contains(ruleSet))
        {
            throw new InvalidOperationException($"The embedded rule catalog overrides the rule '{ruleId}' under the rule set '{entry.RuleSet}', which does not check it.");
        }

        var syntax = entry.Syntax is null
            ? (InvoiceSyntax?)null
            : SyntaxNames.TryGetValue(entry.Syntax, out var named)
                ? named
                : throw new InvalidOperationException($"The embedded rule catalog names the unknown syntax '{entry.Syntax}'.");

        return new SeverityOverride(ruleSet, syntax, SeverityNamed(entry.Severity), entry.Reason);
    }

    /// <summary>The rule set a name in <c>rules.json</c> stands for.</summary>
    private static RuleSet RuleSetNamed(string name) => RuleSetCatalog.TryParseRuleSet(name, out var ruleSet)
        ? ruleSet
        : throw new InvalidOperationException($"The embedded rule catalog names the unknown rule set '{name}'.");

    /// <summary>The severity a name in <c>rules.json</c> stands for.</summary>
    private static FindingSeverity SeverityNamed(string name) => SeverityNames.TryGetValue(name, out var severity)
        ? severity
        : throw new InvalidOperationException($"The embedded rule catalog names the unknown severity '{name}'.");

    /// <summary>The shape of <c>rules.json</c>.</summary>
    /// <param name="ModuleId">The module the rules belong to, which names their reference page; the module itself does not read it.</param>
    /// <param name="Groups">The groups of rules.</param>
    private sealed record CatalogFile(string ModuleId, IReadOnlyList<GroupEntry> Groups);

    /// <summary>A group of rules in <c>rules.json</c>, taken from one standard and checked under the same rule sets.</summary>
    /// <param name="Name">See <see cref="RuleDefinition.Group"/>.</param>
    /// <param name="Standard">See <see cref="RuleDefinition.Standard"/>.</param>
    /// <param name="RuleSets">The names of <see cref="RuleDefinition.RuleSets"/>, as the rule-set catalog spells them.</param>
    /// <param name="Rules">The rules of the group.</param>
    private sealed record GroupEntry(string Name, string Standard, IReadOnlyList<string> RuleSets, IReadOnlyList<RuleEntry> Rules);

    /// <summary>A rule in <c>rules.json</c>; the members are those of <see cref="RuleDefinition"/>, severities by name.</summary>
    /// <param name="Id">See <see cref="RuleDefinition.Id"/>.</param>
    /// <param name="BusinessTerm">See <see cref="RuleDefinition.BusinessTerm"/>.</param>
    /// <param name="Severity">See <see cref="RuleDefinition.Severity"/>.</param>
    /// <param name="SeverityInUbl">See <see cref="RuleDefinition.SeverityInUbl"/>.</param>
    /// <param name="SeverityInCii">See <see cref="RuleDefinition.SeverityInCii"/>.</param>
    /// <param name="Message">See <see cref="RuleDefinition.Message"/>.</param>
    /// <param name="Overrides">See <see cref="RuleDefinition.Overrides"/>.</param>
    private sealed record RuleEntry(
        string Id,
        string? BusinessTerm,
        string Severity,
        string? SeverityInUbl,
        string? SeverityInCii,
        string Message,
        IReadOnlyList<OverrideEntry>? Overrides);

    /// <summary>An override in <c>rules.json</c>; the members are those of <see cref="SeverityOverride"/>, by name.</summary>
    /// <param name="RuleSet">See <see cref="SeverityOverride.RuleSet"/>.</param>
    /// <param name="Syntax">See <see cref="SeverityOverride.Syntax"/>.</param>
    /// <param name="Severity">See <see cref="SeverityOverride.Severity"/>.</param>
    /// <param name="Reason">See <see cref="SeverityOverride.Reason"/>.</param>
    private sealed record OverrideEntry(string RuleSet, string? Syntax, string Severity, string Reason);
}
