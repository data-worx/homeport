// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Shared.Client.ModuleContract;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>Reports to the gateway how much a request counts.</summary>
public static class HomeportUsage
{
    // The separators of the report, and white space, which the header trims.
    private const string ReservedCharacters = "=, \t";

    /// <summary>
    /// Counts the request as <paramref name="quantity"/> in <paramref name="meter"/>
    /// instead of the quantity its route declares, through the
    /// <see cref="ModuleHeaders.Usage"/> header. A response reports one meter,
    /// so a second call replaces the first. The gateway removes the header
    /// before the caller sees it, and falls back to the route's quantity when
    /// <paramref name="meter"/> is not the route's own.
    /// </summary>
    /// <param name="response">The response to the request being counted, before it has started.</param>
    /// <param name="meter">The meter the route declares.</param>
    /// <param name="quantity">How much the request counts; positive.</param>
    /// <exception cref="ArgumentException"><paramref name="meter"/> is empty or contains a character the header cannot carry.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    public static void ReportUsage(this HttpResponse response, string meter, int quantity)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(meter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (meter.AsSpan().ContainsAny(ReservedCharacters))
        {
            throw new ArgumentException("A meter contains no '=', no ',' and no white space.", nameof(meter));
        }

        response.Headers[ModuleHeaders.Usage] = string.Create(CultureInfo.InvariantCulture, $"{meter}={quantity}");
    }
}
