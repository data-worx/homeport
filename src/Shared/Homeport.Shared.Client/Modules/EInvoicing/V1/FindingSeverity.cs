// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>How much a finding weighs; on the wire <c>warning</c>, <c>fatal</c> or <c>information</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FindingSeverity>))]
public enum FindingSeverity
{
    /// <summary>Worth knowing; the document is processed as it is.</summary>
    [JsonStringEnumMemberName("warning")]
    Warning,

    /// <summary>The document breaks a rule; it is no valid e-invoice.</summary>
    [JsonStringEnumMemberName("fatal")]
    Fatal,

    /// <summary>A note on the check itself, such as findings left unreported.</summary>
    [JsonStringEnumMemberName("information")]
    Information,
}
