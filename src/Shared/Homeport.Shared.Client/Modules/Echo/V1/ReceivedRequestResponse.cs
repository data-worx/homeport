// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.Echo.V1;

/// <summary>A request as Echo received it.</summary>
/// <param name="Headers">Every header but <c>Authorization</c>, by lower-case name, values comma-joined.</param>
public sealed record ReceivedRequestResponse(IReadOnlyDictionary<string, string> Headers);
