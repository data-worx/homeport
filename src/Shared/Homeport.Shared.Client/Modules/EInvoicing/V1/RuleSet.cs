// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>
/// The rule set an invoice claims with its specification identifier (BT-24);
/// on the wire <c>en16931</c>, <c>xrechnung</c>, <c>xrechnung-extension</c>,
/// <c>factur-x-basic</c> or <c>factur-x-extended</c>.
/// </summary>
/// <remarks>
/// The ZUGFeRD / Factur-X profiles EN 16931 and XRECHNUNG carry the
/// identifiers of EN 16931 and XRechnung, so they are reported as these.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<RuleSet>))]
public enum RuleSet
{
    /// <summary>EN 16931 itself, without a further specification.</summary>
    [JsonStringEnumMemberName("en16931")]
    En16931,

    /// <summary>XRechnung, the German CIUS of EN 16931.</summary>
    [JsonStringEnumMemberName("xrechnung")]
    XRechnung,

    /// <summary>The XRechnung extension.</summary>
    [JsonStringEnumMemberName("xrechnung-extension")]
    XRechnungExtension,

    /// <summary>The ZUGFeRD / Factur-X profile BASIC.</summary>
    [JsonStringEnumMemberName("factur-x-basic")]
    FacturXBasic,

    /// <summary>The ZUGFeRD / Factur-X profile EXTENDED.</summary>
    [JsonStringEnumMemberName("factur-x-extended")]
    FacturXExtended,
}
