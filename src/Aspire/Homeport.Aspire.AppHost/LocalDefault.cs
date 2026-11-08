// SPDX-License-Identifier: AGPL-3.0-or-later

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Homeport.Aspire.AppHost;

/// <summary>
/// A fixed value for a local run that configuration overrides, unlike a
/// parameter added with a value, whose value is fixed for good: a run sets
/// <c>Parameters:&lt;name&gt;</c> in its user secrets or on its command line,
/// as the end-to-end tests do for the seeded plan.
/// </summary>
/// <param name="value">The value of the parameter while nothing configures one.</param>
internal sealed class LocalDefault(string value) : ParameterDefault
{
    public override string GetDefaultValue()
    {
        return value;
    }

    // Writes no default: a published deployment takes every value from its
    // environment, as it did before the parameter had a default.
    public override void WriteToManifest(ManifestPublishingContext context)
    {
    }
}
