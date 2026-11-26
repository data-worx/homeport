// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>An element a business rule is checked on, with the JSON path a finding on it points at.</summary>
/// <param name="Element">The element, the context of the rule in its schematron.</param>
/// <param name="Path">The path of the entry of the model the element is read into; of the nearest enclosing entry when the model keeps none for it.</param>
/// <param name="InModel">Whether the model keeps an entry for the element itself, so a member of it can be named.</param>
internal readonly record struct RuleContext(XElement Element, string Path, bool InModel)
{
    /// <summary>The path of <paramref name="member"/> of the entry; of the enclosing entry when the model keeps none for the element.</summary>
    /// <param name="member">The member, as the model's wire form names it.</param>
    public string PathTo(string member) => InModel ? $"{Path}.{member}" : Path;
}
