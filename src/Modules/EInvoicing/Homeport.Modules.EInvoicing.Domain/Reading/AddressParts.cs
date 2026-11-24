// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Reading;

/// <summary>The parts of a postal address, which the four address groups of EN 16931 share.</summary>
/// <param name="Line1">Address line 1.</param>
/// <param name="Line2">Address line 2.</param>
/// <param name="Line3">Address line 3.</param>
/// <param name="City">City.</param>
/// <param name="PostCode">Post code.</param>
/// <param name="Subdivision">Country subdivision.</param>
/// <param name="CountryCode">Country code.</param>
internal sealed record AddressParts(
    string? Line1,
    string? Line2,
    string? Line3,
    string? City,
    string? PostCode,
    string? Subdivision,
    string? CountryCode);
