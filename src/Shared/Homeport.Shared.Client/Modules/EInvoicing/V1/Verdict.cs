// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>What the module concludes about a processed document; on the wire <c>not-an-e-invoice</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Verdict>))]
public enum Verdict
{
    /// <summary>An invoice, but no e-invoice under EN 16931: ZUGFeRD MINIMUM, BASIC WL or 1.0.</summary>
    [JsonStringEnumMemberName("not-an-e-invoice")]
    NotAnEInvoice,
}
