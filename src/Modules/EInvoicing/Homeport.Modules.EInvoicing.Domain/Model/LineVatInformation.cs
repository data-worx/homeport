// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The VAT of the item of a line (BG-30).</summary>
public sealed record LineVatInformation
{
    /// <summary>Invoiced item VAT category code (BT-151).</summary>
    [BusinessTerm("BT-151")]
    public string? VatCategoryCode { get; init; }

    /// <summary>Invoiced item VAT rate (BT-152).</summary>
    [BusinessTerm("BT-152")]
    public string? VatRate { get; init; }
}
