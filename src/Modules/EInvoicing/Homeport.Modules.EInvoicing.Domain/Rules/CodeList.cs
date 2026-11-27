// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>A code list a code-list rule checks a code against, as the schematron of EN 16931 tests it.</summary>
public sealed class CodeList
{
    // The text the schematron searches: every code between single spaces.
    private readonly string text;

    /// <summary>Creates a list of the given codes.</summary>
    /// <param name="name">See <see cref="Name"/>.</param>
    /// <param name="codes">The codes, in the order the schematron lists them.</param>
    public CodeList(string name, IEnumerable<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        Name = name;
        Codes = [.. codes];
        text = $" {string.Join(' ', Codes)} ";
    }

    /// <summary>The name of the list, such as <c>ISO 4217</c>.</summary>
    public string Name { get; }

    /// <summary>The codes, in the order the schematron lists them.</summary>
    public IReadOnlyList<string> Codes { get; }

    /// <summary>Whether the value is a code of the list, as the schematron tests it.</summary>
    /// <remarks>
    /// The test is <c>not(contains(normalize-space(v), ' ')) and
    /// contains(' list ', concat(' ', normalize-space(v), ' '))</c>: the
    /// value with its whitespace normalised is one whole code, compared
    /// exactly. A blank value, as an absent one, is none.
    /// </remarks>
    /// <param name="value">The value, if there is one.</param>
    public bool Contains(string? value)
    {
        var code = RuleXml.NormalizeSpace(value);

        return code.Length > 0 && !code.Contains(' ', StringComparison.Ordinal) && text.Contains($" {code} ", StringComparison.Ordinal);
    }

    /// <summary>Whether the text of the list contains <paramref name="value"/> anywhere, as XPath's <c>contains</c> on the list alone tests it.</summary>
    /// <remarks>
    /// UBL's test of BR-CL-08 searches the list for the code it cuts out of a
    /// note, without spaces around it, so a part of a code, or one that spans
    /// two codes, is found as well.
    /// </remarks>
    /// <param name="value">The value searched for.</param>
    public bool TextContains(string value) => text.Contains(value, StringComparison.Ordinal);
}
