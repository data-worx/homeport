// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.Gateway.Domain.Usage;

/// <summary>
/// The rules for a quantity a module reports for a request as
/// <c>&lt;meter&gt;=&lt;quantity&gt;</c>, instead of the default of the
/// route it was matched on. A report that breaks one is rejected, and the
/// route's default applies.
/// </summary>
public static class ReportedUsage
{
    /// <summary>The report is not one <c>&lt;meter&gt;=&lt;quantity&gt;</c> pair.</summary>
    public const string MalformedCode = "usage.malformed";

    /// <summary>The report names a meter other than the one the route declares.</summary>
    public const string UndeclaredMeterCode = "usage.undeclared-meter";

    /// <summary>The reported quantity is not a positive integer.</summary>
    public const string InvalidQuantityCode = "usage.invalid-quantity";

    private const char Separator = '=';

    // Several values of one header arrive joined by a comma, and a response
    // reports at most one meter.
    private const char ValueSeparator = ',';

    /// <summary>
    /// The quantity <paramref name="report"/> states for <paramref name="routeMeter"/>,
    /// or why it is rejected.
    /// </summary>
    /// <param name="report">The report, as <c>&lt;meter&gt;=&lt;quantity&gt;</c>.</param>
    /// <param name="routeMeter">The meter the matched route declares.</param>
    public static Result<int> QuantityOf(string report, string routeMeter)
    {
        var separator = report.IndexOf(Separator, StringComparison.Ordinal);
        if (separator <= 0 || report.Contains(ValueSeparator, StringComparison.Ordinal))
        {
            return Result<int>.Failure(new Error(MalformedCode, "A usage report is one '<meter>=<quantity>' pair."));
        }

        var meter = report[..separator];
        if (!string.Equals(meter, routeMeter, StringComparison.Ordinal))
        {
            return Result<int>.Failure(new Error(UndeclaredMeterCode, $"The route counts in the meter '{routeMeter}', not in '{meter}'."));
        }

        if (!int.TryParse(report.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) || quantity == 0)
        {
            return Result<int>.Failure(new Error(InvalidQuantityCode, "A reported quantity is a positive integer."));
        }

        return Result<int>.Success(quantity);
    }
}
