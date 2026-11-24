// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A business term of the data type Binary object, described by its metadata; its content is not kept.</summary>
/// <param name="MimeCode">The media type of the content, as the document declares it; none when it declares none.</param>
/// <param name="Filename">The file name of the content, as the document declares it; none when it declares none.</param>
public sealed record BinaryObject(string? MimeCode, string? Filename);
