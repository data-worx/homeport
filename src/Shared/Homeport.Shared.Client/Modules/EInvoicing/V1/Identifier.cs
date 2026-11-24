// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A business term of the data type Identifier: a value, and the scheme it is taken from where the document names one.</summary>
/// <param name="Value">The identifier, as the document writes it.</param>
/// <param name="SchemeId">The identification scheme; <see langword="null"/> when the document names none.</param>
/// <param name="SchemeVersionId">The version of the scheme; <see langword="null"/> when the document names none.</param>
public sealed record Identifier(string Value, string? SchemeId, string? SchemeVersionId);
