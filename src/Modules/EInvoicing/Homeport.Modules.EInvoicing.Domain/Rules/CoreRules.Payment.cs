// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The core rules on the payment instructions (BG-16). They are checked in
/// the document: both syntaxes repeat the payment means for each account,
/// which the model gathers into one.
/// </summary>
public static partial class CoreRules
{
    /// <summary>The path of the payment instructions.</summary>
    private const string PaymentInstructionsPath = "$.paymentInstructions";

    /// <summary>BR-49: every payment means carries a payment means type code (BT-81).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br49(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var code = input.IsCii ? Ram + "TypeCode" : Cbc + "PaymentMeansCode";

        return PaymentMeans(input)
            .Where(means => means.Element(code) is null)
            .Select(_ => $"{PaymentInstructionsPath}.paymentMeansTypeCode");
    }

    /// <summary>BR-50: the account of every credit transfer carries a payment account identifier (BT-84).</summary>
    /// <remarks>
    /// The account of a payment means of the type 30 or 58, the code compared
    /// exactly, carries an identifier that is not blank: in UBL its
    /// identifier, in CII its IBAN or its proprietary identifier.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br50(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return CreditTransferAccounts(input)
            .Where(account => input.IsCii
                ? IsBlank(account.Element(Ram + "IBANID")) && IsBlank(account.Element(Ram + "ProprietaryID"))
                : IsBlank(account.Element(Cbc + "ID")))
            .Select(account => $"{AccountPathOf(input, account)}.paymentAccountIdentifier");
    }

    /// <summary>BR-51: the payment card primary account number (BT-87) has at most 10 characters.</summary>
    /// <remarks>UBL weighs a longer number as a warning, CII as fatal; the rule catalog carries both.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br51(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const int maxLength = 10;
        var numbers = input.IsCii
            ? input.Root.Descendants(Ram + "ApplicableTradeSettlementFinancialCard").Select(card => card.Element(Ram + "ID"))
            : input.Root.Descendants(Cac + "PaymentMeans").Elements(Cac + "CardAccount").Elements(Cbc + "PrimaryAccountNumberID");

        return numbers
            .Where(number => StringLength(NormalizeSpace(number?.Value)) > maxLength)
            .Select(_ => $"{PaymentInstructionsPath}.paymentCard.primaryAccountNumber");
    }

    /// <summary>BR-61: a credit transfer carries the payment account identifier (BT-84).</summary>
    /// <remarks>
    /// UBL asks every payment means whose code, trimmed, is 30 or 58 for an
    /// account with an identifier. CII asks only an account that is there,
    /// below a payment means of that type, for an IBAN or a proprietary
    /// identifier, blank or not.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br61(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = $"{PaymentInstructionsPath}.creditTransfers";
        if (input.IsCii)
        {
            return CreditTransferAccounts(input)
                .Where(account => account.Element(Ram + "IBANID") is null && account.Element(Ram + "ProprietaryID") is null)
                .Select(account => $"{AccountPathOf(input, account)}.paymentAccountIdentifier");
        }

        return PaymentMeans(input)
            .Where(means => CreditTransferCodes.Contains(NormalizeSpace(means.Element(Cbc + "PaymentMeansCode")?.Value), StringComparer.Ordinal)
                && !means.Elements(Cac + "PayeeFinancialAccount").Elements(Cbc + "ID").Any())
            .Select(_ => path);
    }

    /// <summary>Every payment means of the document.</summary>
    private static IEnumerable<XElement> PaymentMeans(RuleInput input) =>
        input.Root.Descendants(input.IsCii ? Ram + "SpecifiedTradeSettlementPaymentMeans" : Cac + "PaymentMeans");

    /// <summary>The payee's accounts below payment means whose code is exactly 30 or 58.</summary>
    private static IEnumerable<XElement> CreditTransferAccounts(RuleInput input) => PaymentMeans(input)
        .Where(means => IsOneOf(means.Element(input.IsCii ? Ram + "TypeCode" : Cbc + "PaymentMeansCode"), CreditTransferCodes))
        .Elements(input.IsCii ? Ram + "PayeePartyCreditorFinancialAccount" : Cac + "PayeeFinancialAccount");

    /// <summary>The path of the credit transfer (BG-17) the model reads from <paramref name="account"/>.</summary>
    private static string AccountPathOf(RuleInput input, XElement account)
    {
        var accounts = PaymentMeans(input)
            .Elements(input.IsCii ? Ram + "PayeePartyCreditorFinancialAccount" : Cac + "PayeeFinancialAccount")
            .ToList();

        return RulePaths.Item($"{PaymentInstructionsPath}.creditTransfers", IndexOf(accounts, account));
    }
}
