// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The code-list rules on the document as a whole: its type, currencies, dates, references, notes, payment and attachments.</summary>
public static partial class CodeListRules
{
    // The document type code UBL's BR-CL-07 asks for: an invoiced object identifier (BT-18).
    private const string InvoicedObjectTypeCode = "130";

    // The character that encloses the subject code at the start of a UBL note.
    private const char NoteSubjectDelimiter = '#';

    // The length of a subject code of UNTDID 4451.
    private const int SubjectCodeLength = 3;

    // The amounts whose currency UBL's BR-CL-03 checks, wherever they occur.
    private static readonly XName[] UblAmounts =
    [
        Cbc + "Amount", Cbc + "BaseAmount", Cbc + "PriceAmount", Cbc + "TaxAmount", Cbc + "TaxableAmount",
        Cbc + "LineExtensionAmount", Cbc + "TaxExclusiveAmount", Cbc + "TaxInclusiveAmount", Cbc + "AllowanceTotalAmount",
        Cbc + "ChargeTotalAmount", Cbc + "PrepaidAmount", Cbc + "PayableRoundingAmount", Cbc + "PayableAmount",
    ];

    /// <summary>BR-CL-01: the invoice type code (BT-3) is a code of UNTDID 1001.</summary>
    /// <remarks>
    /// UBL checks an invoice type code against the codes of an invoice and a
    /// credit note type code against those of a credit note; CII checks the
    /// type code of the exchanged document against both.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl01(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            return NotCoded(Below(input, Rsm + "ExchangedDocument", Ram + "TypeCode"), CodeLists.Untdid1001Cii, _ => "$.typeCode");
        }

        return NotCoded(input.Root.Descendants(Cbc + "InvoiceTypeCode"), CodeLists.Untdid1001UblInvoice, _ => "$.typeCode")
            .Concat(NotCoded(input.Root.Descendants(Cbc + "CreditNoteTypeCode"), CodeLists.Untdid1001UblCreditNote, _ => "$.typeCode"));
    }

    /// <summary>BR-CL-03: the currency of an amount is a code of ISO 4217.</summary>
    /// <remarks>
    /// UBL checks the currency of every amount of <see cref="UblAmounts"/>,
    /// whether or not it carries one; CII checks only the total VAT amount
    /// that carries one, the one amount of CII with a currency.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl03(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var amounts = input.IsCii
            ? input.Root.Descendants(Ram + "TaxTotalAmount").WithAttribute(CurrencyIdAttribute)
            : input.Root.Descendants().Where(element => UblAmounts.Contains(element.Name));

        return NotCoded(amounts, CurrencyIdAttribute, CodeLists.Iso4217, element => LineOr(input, element, input.IsCii ? "$.documentTotals" : RulePaths.Invoice));
    }

    /// <summary>BR-CL-04: the invoice currency code (BT-5) is a code of ISO 4217.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl04(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return NotCoded(input.Root.Descendants(input.IsCii ? Ram + "InvoiceCurrencyCode" : Cbc + "DocumentCurrencyCode"), CodeLists.Iso4217, _ => "$.currencyCode");
    }

    /// <summary>BR-CL-05: the VAT accounting currency code (BT-6) is a code of ISO 4217.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl05(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return NotCoded(input.Root.Descendants((input.IsCii ? Ram : Cbc) + "TaxCurrencyCode"), CodeLists.Iso4217, _ => "$.vatAccountingCurrencyCode");
    }

    /// <summary>BR-CL-06: the value added tax point date code (BT-8) is a code of the restriction of UNTDID 2005 its syntax accepts.</summary>
    /// <remarks>
    /// UBL checks the description code of every invoice period, an invoice
    /// line's included, against <c>3 35 432</c>; CII checks every due date
    /// type code against <c>5 29 72</c>.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl06(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.IsCii
            ? NotCoded(input.Root.Descendants(Ram + "DueDateTypeCode"), CodeLists.Untdid2005Cii, element => LineOr(input, element, "$.vatPointDateCode"))
            : NotCoded(Below(input, Cac + "InvoicePeriod", Cbc + "DescriptionCode"), CodeLists.Untdid2005Ubl, element => LineOr(input, element, "$.vatPointDateCode"));
    }

    /// <summary>BR-CL-07: the scheme of the invoiced object identifier (BT-18) is a code of UNTDID 1153.</summary>
    /// <remarks>
    /// UBL checks the scheme of the identifier of every document reference of
    /// type <c>130</c> that carries one; CII checks every reference type code.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl07(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            return NotCoded(input.Root.Descendants(Ram + "ReferenceTypeCode"), CodeLists.Untdid1153, element => LineOr(input, element, "$.invoicedObjectIdentifier"));
        }

        var identifiers = input.Root.Descendants()
            .Where(reference => reference.Name == Cac + "AdditionalDocumentReference" || reference.Name == Cac + "DocumentReference")
            .Where(reference => reference.Elements(Cbc + "DocumentTypeCode").Any(code => code.Value == InvoicedObjectTypeCode))
            .Elements(Cbc + "ID")
            .WithAttribute(SchemeIdAttribute);

        return NotCoded(identifiers, SchemeIdAttribute, CodeLists.Untdid1153, element => LineOr(input, element, "$.invoicedObjectIdentifier"));
    }

    /// <summary>BR-CL-08: the subject code of an invoice note (BT-21) is a code of UNTDID 4451.</summary>
    /// <remarks>
    /// CII checks every subject code. UBL writes the code at the start of
    /// the note, between two <c>#</c>: it checks only a note with three
    /// characters between its first two <c>#</c>, and searches the list for
    /// them as <see cref="CodeList.TextContains"/> does.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl08(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var list = CodeLists.Named(CodeLists.Untdid4451);
        if (input.IsCii)
        {
            var notes = input.Root.Element(Rsm + "ExchangedDocument")?.Elements(Ram + "IncludedNote").ToList() ?? [];

            return input.Root.Descendants(Ram + "SubjectCode")
                .Where(code => !list.Contains(code.Value))
                .Select(code => code.Parent is { } note && IndexOf(notes, note) is var index and >= 0
                    ? RulePaths.Item("$.notes", index)
                    : input.LinePathOf(code));
        }

        return input.Root.Elements(Cbc + "Note")
            .Select((note, index) => (Code: SubjectCodeOf(note.Value), Index: index))
            .Where(note => note.Code is not null && StringLength(note.Code) == SubjectCodeLength && !list.TextContains(note.Code))
            .Select(note => RulePaths.Item("$.notes", note.Index));
    }

    /// <summary>BR-CL-16: the payment means type code (BT-81) is a code of UNTDID 4461.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl16(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? Below(input, Ram + "SpecifiedTradeSettlementPaymentMeans", Ram + "TypeCode")
            : Below(input, Cac + "PaymentMeans", Cbc + "PaymentMeansCode");

        return NotCoded(codes, CodeLists.Untdid4461, _ => "$.paymentInstructions");
    }

    /// <summary>BR-CL-22: a VAT exemption reason code (BT-121) is a code of the VATEX list.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl22(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.Root.Descendants(input.IsCii ? Ram + "ExemptionReasonCode" : Cbc + "TaxExemptionReasonCode");

        return NotCoded(codes, CodeLists.Vatex, input.LinePathOf);
    }

    /// <summary>BR-CL-24: the MIME code of an attached document (BT-125) is one of the MIME media types EN 16931 allows.</summary>
    /// <remarks>Checked on every attachment that carries a MIME code, compared exactly, without normalising its whitespace.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl24(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var mimeCode = XName.Get("mimeCode");
        var types = CodeLists.Named(CodeLists.MimeMediaTypes).Codes;

        return input.Root.Descendants(input.IsCii ? Ram + "AttachmentBinaryObject" : Cbc + "EmbeddedDocumentBinaryObject")
            .Where(attachment => attachment.Attribute(mimeCode) is { } code && !types.Contains(code.Value, StringComparer.Ordinal))
            .Select(input.LinePathOf);
    }

    /// <summary>The text between the first two <c>#</c> of a UBL note, as <c>substring-before(substring-after(., '#'), '#')</c> cuts it; none without a <c>#</c>.</summary>
    private static string? SubjectCodeOf(string note)
    {
        var start = note.IndexOf(NoteSubjectDelimiter, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var rest = note[(start + 1)..];
        var end = rest.IndexOf(NoteSubjectDelimiter, StringComparison.Ordinal);

        return end < 0 ? string.Empty : rest[..end];
    }
}
