// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>A request as the stand-in module received it.</summary>
/// <param name="Path">The path below the module.</param>
/// <param name="Headers">Every header by lower-case name, values comma-joined.</param>
public sealed record ForwardedRequest(string Path, Dictionary<string, string> Headers);
