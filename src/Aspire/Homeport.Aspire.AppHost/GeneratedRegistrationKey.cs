// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Homeport.Aspire.AppHost;

/// <summary>
/// Generates a module's registration key for a local run: 32 random bytes,
/// hex-encoded, kept in the user secrets like the generated passwords.
/// </summary>
internal sealed class GeneratedRegistrationKey : ParameterDefault
{
    private const int KeyBytes = 32;

    public override string GetDefaultValue()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(KeyBytes));
    }

    // Writes no default: a published deployment never gets a generated key,
    // the operator supplies their own (ADR-0012).
    public override void WriteToManifest(ManifestPublishingContext context)
    {
    }
}
