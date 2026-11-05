// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Proxies;

/// <summary>Keeps the gateway from starting with a trusted proxy it cannot read.</summary>
internal sealed class TrustedProxiesOptionsValidator : IValidateOptions<TrustedProxiesOptions>
{
    public ValidateOptionsResult Validate(string? name, TrustedProxiesOptions options)
    {
        var failures = new List<string>();

        foreach (var proxy in options.Proxies.Where(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            failures.Add($"{TrustedProxiesOptions.SectionName}:{nameof(options.Proxies)} must hold IP addresses; '{proxy}' is none.");
        }

        foreach (var network in options.Networks.Where(network => !IPNetwork.TryParse(network, out _)))
        {
            failures.Add($"{TrustedProxiesOptions.SectionName}:{nameof(options.Networks)} must hold networks in CIDR notation; '{network}' is none.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
