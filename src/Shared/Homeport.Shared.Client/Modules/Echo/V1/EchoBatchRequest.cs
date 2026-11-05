// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Homeport.Shared.Client.Modules.Echo.V1;

/// <summary>A batch of items for Echo to return.</summary>
/// <param name="Items">The items, any JSON values; at least one.</param>
public sealed record EchoBatchRequest(IReadOnlyList<JsonElement> Items);
