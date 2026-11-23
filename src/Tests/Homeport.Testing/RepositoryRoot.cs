// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing;

/// <summary>The root of the repository checkout the tests run in, found by its <c>.git</c>.</summary>
public static class RepositoryRoot
{
    /// <summary>The full path of the repository root.</summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No .git found above {AppContext.BaseDirectory}; the test needs the repository checkout.");
    }
}
