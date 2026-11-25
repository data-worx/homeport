// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The JSON paths in the semantic model a finding of a business rule points at.</summary>
/// <remarks>
/// A path names the members as the model's wire form does, so a caller of
/// the read endpoint finds the term the finding concerns in what it read.
/// </remarks>
internal static class RulePaths
{
    /// <summary>The invoice as a whole.</summary>
    public const string Invoice = "$";

    /// <summary>The invoice line (BG-25) at <paramref name="index"/>.</summary>
    /// <param name="index">The position of the line, from 0.</param>
    public static string Line(int index) => Item("$.lines", index);

    /// <summary>The entry at <paramref name="index"/> of the list at <paramref name="list"/>.</summary>
    /// <param name="list">The path of the list.</param>
    /// <param name="index">The position of the entry, from 0.</param>
    public static string Item(string list, int index) => string.Create(CultureInfo.InvariantCulture, $"{list}[{index}]");
}
