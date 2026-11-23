// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>How much a finding weighs; on the wire <c>warning</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FindingSeverity>))]
public enum FindingSeverity
{
    /// <summary>Worth knowing; the document is processed as it is.</summary>
    [JsonStringEnumMemberName("warning")]
    Warning,
}
