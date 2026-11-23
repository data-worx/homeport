// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>What a document arrived as; on the wire <c>xml</c> or <c>pdf</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DocumentContainer>))]
public enum DocumentContainer
{
    /// <summary>An XML document, the invoice itself.</summary>
    [JsonStringEnumMemberName("xml")]
    Xml,

    /// <summary>A PDF, which may carry an invoice as an embedded file.</summary>
    [JsonStringEnumMemberName("pdf")]
    Pdf,
}
