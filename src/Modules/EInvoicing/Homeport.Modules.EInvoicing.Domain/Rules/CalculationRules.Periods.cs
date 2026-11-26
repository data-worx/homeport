// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The calculation rules on the invoicing period (BG-14) and the invoice line period (BG-26).</summary>
public static partial class CalculationRules
{
    /// <summary>BR-CO-19: an invoicing period carries a start date (BT-73) or an end date (BT-74).</summary>
    /// <remarks>
    /// Checked on the periods <see cref="RuleContexts"/> names. UBL accepts a
    /// period that carries a VAT point date code (BT-8) alone, CII does not:
    /// it carries that code elsewhere.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo19(RuleInput input) =>
        RuleContexts.InvoicingPeriods(input)
            .Where(context => !(input.IsCii
                ? HasAny(context.Element, Ram + "StartDateTime", Ram + "EndDateTime")
                : HasAny(context.Element, Cbc + "StartDate", Cbc + "EndDate", Cbc + "DescriptionCode")))
            .Select(context => context.Path);

    /// <summary>BR-CO-20: an invoice line period carries a start date (BT-134) or an end date (BT-135).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo20(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var periods = input.IsCii
            ? input.Root.Descendants(Ram + "SpecifiedLineTradeSettlement").Elements(Ram + "BillingSpecifiedPeriod")
            : input.Root.Descendants().Where(line => line.Name == Cac + "InvoiceLine" || line.Name == Cac + "CreditNoteLine").Elements(Cac + "InvoicePeriod");

        return periods
            .Where(period => !(input.IsCii
                ? HasAny(period, Ram + "StartDateTime", Ram + "EndDateTime")
                : HasAny(period, Cbc + "StartDate", Cbc + "EndDate")))
            .Select(period => input.LinePathOf(period) is var line && line == RulePaths.Invoice ? line : $"{line}.period");
    }

    /// <summary>Whether the element has a child of any of the names.</summary>
    private static bool HasAny(XElement element, params XName[] names) => names.Any(name => element.Element(name) is not null);
}
