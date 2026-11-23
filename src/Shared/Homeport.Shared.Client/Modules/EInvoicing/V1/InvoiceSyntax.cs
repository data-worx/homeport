// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>
/// The syntax an invoice is written in, one of those EN 16931 binds to; on the
/// wire <c>ubl-invoice</c>, <c>ubl-credit-note</c> or <c>cii</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InvoiceSyntax>))]
public enum InvoiceSyntax
{
    /// <summary>An invoice in OASIS UBL 2.1, root element <c>Invoice</c>.</summary>
    [JsonStringEnumMemberName("ubl-invoice")]
    UblInvoice,

    /// <summary>A credit note in OASIS UBL 2.1, root element <c>CreditNote</c>.</summary>
    [JsonStringEnumMemberName("ubl-credit-note")]
    UblCreditNote,

    /// <summary>A cross industry invoice in UN/CEFACT CII, root element <c>CrossIndustryInvoice</c>.</summary>
    [JsonStringEnumMemberName("cii")]
    Cii,
}
