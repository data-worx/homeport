namespace Architecture.Tests;

/// <summary>One <c>.csproj</c> under <c>src/</c>, with what it references.</summary>
/// <param name="Name">The file name without its extension.</param>
/// <param name="RelativeDirectory">The directory, relative to <c>src/</c>, with <c>/</c> as separator.</param>
/// <param name="Location">What the directory makes the project.</param>
/// <param name="ProjectReferences">The names of the projects it references.</param>
/// <param name="PackageReferences">The names of the packages it references.</param>
internal sealed record ProjectFile(
    string Name,
    string RelativeDirectory,
    ProjectLocation Location,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences)
{
    internal bool IsDomain => Name.EndsWith(".Domain", StringComparison.Ordinal);

    internal bool IsApplication => Name.EndsWith(".Application", StringComparison.Ordinal);

    internal bool IsInfrastructure => Name.Contains(".Infrastructure", StringComparison.Ordinal);

    internal bool IsLayer => IsDomain || IsApplication || IsInfrastructure;
}
