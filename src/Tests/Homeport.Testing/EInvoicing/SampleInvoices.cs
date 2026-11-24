// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// Invoices written for the tests of the e-invoicing module, embedded from
/// <c>EInvoicing/Invoices/</c>. Each comes in UBL and in CII with the same
/// content, so a test can compare what the module reads from either.
/// </summary>
public static class SampleInvoices
{
    /// <summary>An invoice carrying every business term of EN 16931 once, in UBL.</summary>
    public const string MaximalUbl = "maximal-invoice.ubl.xml";

    /// <summary>The invoice of <see cref="MaximalUbl"/> in CII.</summary>
    public const string MaximalCii = "maximal-invoice.cii.xml";

    /// <summary>A credit note carrying what a UBL CreditNote binds differently from an Invoice, in UBL.</summary>
    public const string CreditNoteUbl = "credit-note.ubl.xml";

    /// <summary>The credit note of <see cref="CreditNoteUbl"/> in CII.</summary>
    public const string CreditNoteCii = "credit-note.cii.xml";

    private const string ResourcePrefix = "Homeport.Testing.EInvoicing.Invoices.";

    /// <summary>The invoice named <paramref name="name"/>, as text.</summary>
    public static string Text(string name)
    {
        using var stream = typeof(SampleInvoices).Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException($"Homeport.Testing carries no sample invoice '{name}'.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
