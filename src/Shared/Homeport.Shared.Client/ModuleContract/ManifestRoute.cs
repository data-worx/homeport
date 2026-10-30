// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.ModuleContract;

/// <summary>One route a module serves, as the manifest declares it.</summary>
/// <param name="Method">The HTTP method, upper case.</param>
/// <param name="Path">The path below the module, starting with <c>/v{major}/</c>.</param>
/// <param name="Scope">The scope a caller needs; one of the manifest's scopes.</param>
/// <param name="Meter">The meter a request is counted in; one of the manifest's meters.</param>
/// <param name="Quantity">How much one request counts, unless the module reports otherwise.</param>
public sealed record ManifestRoute(string Method, string Path, string Scope, string Meter, int Quantity);
