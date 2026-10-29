// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// The references a Domain project may take beyond the BCL (<c>dotnet.layout</c>).
/// </summary>
/// <remarks>
/// Each entry is the name of a project or a package. An entry is an exception,
/// not a convenience: it is allowed only when it runs in-process, and the
/// <i>Architecture</i> section of <c>AGENTS.md</c> points at this file rather
/// than restating the list.
/// </remarks>
internal static class DomainExceptions
{
    /// <summary>
    /// The types a Domain shares with the SDK and the other services. It is an
    /// exception only because it references nothing itself, which
    /// <see cref="ReferenceRules.ContractsReferences"/> asserts.
    /// </summary>
    internal const string Contracts = "Homeport.Shared.Contracts";

    internal static readonly string[] Allowed = [Contracts];
}
