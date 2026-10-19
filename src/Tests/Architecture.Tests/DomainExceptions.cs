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
    internal static readonly string[] Allowed = [];
}
