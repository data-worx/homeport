// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Sdk.Contracts;

/// <summary>An expected business outcome that is not a success.</summary>
/// <param name="Code">
/// A stable business code, such as <c>order.not-found</c>. It knows no HTTP
/// status or problem type; the API layer maps it onto one.
/// </param>
/// <param name="Message">An English description for a developer, not for an end user.</param>
public sealed record Error(string Code, string Message);
