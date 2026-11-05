// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Homeport.Shared.Client.Modules.Echo.V1;

/// <summary>The batch Echo returns; it counts one per item in the meter <c>items</c>.</summary>
/// <param name="Items">The items as they were sent, in the same order.</param>
public sealed record BatchResponse(IReadOnlyList<JsonElement> Items);
