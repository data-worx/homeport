// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An attached document, described by its metadata; the module never returns its content.</summary>
/// <param name="MimeCode">The media type of the content, as the document declares it; <see langword="null"/> when it declares none.</param>
/// <param name="Filename">The file name of the content, as the document declares it; <see langword="null"/> when it declares none.</param>
public sealed record BinaryObject(string? MimeCode, string? Filename);
