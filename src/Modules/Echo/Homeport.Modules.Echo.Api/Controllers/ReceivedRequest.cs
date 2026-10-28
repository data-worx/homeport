// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.Echo.Api.Controllers;

/// <summary>A request as Echo received it.</summary>
/// <param name="Headers">Every header but <c>Authorization</c>, by lower-case name, values comma-joined.</param>
public sealed record ReceivedRequest(IReadOnlyDictionary<string, string> Headers);
