// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Architecture.Tests;

/// <summary>
/// Builds project trees that break a rule on purpose, so a test proves the rule
/// fails when it should rather than only passing on this repository.
/// </summary>
internal static class Fixture
{
    /// <summary>The solution name every fixture project belongs to.</summary>
    internal const string Solution = "Homeport";

    /// <summary>A project named after its directory.</summary>
    /// <param name="path">The directory relative to <c>src/</c>, ending in the project name.</param>
    /// <param name="projectReferences">The names of the projects it references.</param>
    internal static ProjectFile Project(string path, params string[] projectReferences)
    {
        return new ProjectFile(Solution, path.Split('/')[^1], path, projectReferences, [], []);
    }

    /// <summary>A project named after its directory that references packages only.</summary>
    /// <param name="path">The directory relative to <c>src/</c>, ending in the project name.</param>
    /// <param name="packageReferences">The names of the packages it references.</param>
    internal static ProjectFile WithPackages(string path, params string[] packageReferences)
    {
        return new ProjectFile(Solution, path.Split('/')[^1], path, [], packageReferences, []);
    }

    /// <summary>A project named after its directory that references shared frameworks only.</summary>
    /// <param name="path">The directory relative to <c>src/</c>, ending in the project name.</param>
    /// <param name="frameworkReferences">The names of the shared frameworks it references.</param>
    internal static ProjectFile WithFrameworks(string path, params string[] frameworkReferences)
    {
        return new ProjectFile(Solution, path.Split('/')[^1], path, [], [], frameworkReferences);
    }
}
