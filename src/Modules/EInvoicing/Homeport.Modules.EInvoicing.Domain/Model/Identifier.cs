// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A business term of the data type Identifier: a value, and the scheme it is taken from where the document names one.</summary>
/// <param name="Value">The identifier, as the document writes it.</param>
/// <param name="SchemeId">The identification scheme; none when the document names none.</param>
/// <param name="SchemeVersionId">The version of the scheme; none when the document names none.</param>
public sealed record Identifier(string Value, string? SchemeId = null, string? SchemeVersionId = null);
