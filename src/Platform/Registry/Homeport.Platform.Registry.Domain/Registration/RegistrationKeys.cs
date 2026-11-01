// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text;

namespace Homeport.Platform.Registry.Domain.Registration;

/// <summary>The rules of a module's registration keys (ADR-0012).</summary>
public static class RegistrationKeys
{
    /// <summary>How many keys may be valid for one module id at once, so a key rotates without downtime.</summary>
    public const int MaximumPerModule = 2;

    /// <summary>Whether <paramref name="presented"/> is one of <paramref name="valid"/>.</summary>
    /// <remarks>
    /// Compares in constant time. Both sides are hashed first, so neither the
    /// content nor the length of a valid key shows in the time it takes.
    /// Every valid key is compared, whichever matches.
    /// </remarks>
    /// <param name="presented">The key the request carries.</param>
    /// <param name="valid">The keys configured for the module id.</param>
    public static bool Matches(string presented, IEnumerable<string> valid)
    {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var matches = false;

        foreach (var key in valid)
        {
            matches |= CryptographicOperations.FixedTimeEquals(presentedHash, SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        }

        return matches;
    }
}
