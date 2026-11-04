// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>The tokens the gateway issues for the requests it forwards to modules (ADR-0011).</summary>
public sealed class ModuleTokenOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "ModuleTokens";

    /// <summary>How long a module token lives unless configured otherwise.</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long a module token lives. Long enough for one request to reach
    /// the module; at most <see cref="Shared.Authentication.GatewayTokens.MaximumLifetime"/>.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = DefaultLifetime;
}
