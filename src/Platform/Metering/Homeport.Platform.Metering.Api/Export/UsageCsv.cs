// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text;
using Homeport.Shared.Client.Platform.Metering.V1;

namespace Homeport.Platform.Metering.Api.Export;

/// <summary>
/// Writes a <see cref="UsageResponse"/> as CSV (RFC 4180): a header row, one
/// row per entry, CRLF line breaks, and a field quoted where it needs it.
/// </summary>
internal static class UsageCsv
{
    /// <summary>The header row's columns, in order.</summary>
    internal static readonly string[] Columns = ["period", "module", "meter", "quantity"];

    private const string LineBreak = "\r\n";

    private const char Separator = ',';

    private const char Quote = '"';

    private const string EscapedQuote = "\"\"";

    // ISO 8601 in UTC with Z, the same moment the JSON export writes.
    private const string PeriodFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly char[] NeedsQuoting = [Separator, Quote, '\r', '\n'];

    /// <summary>The CSV text of <paramref name="usage"/>.</summary>
    internal static string Write(UsageResponse usage)
    {
        var csv = new StringBuilder();
        AppendRow(csv, Columns);

        foreach (var entry in usage.Usage)
        {
            AppendRow(csv, [
                entry.Period.ToString(PeriodFormat, CultureInfo.InvariantCulture),
                entry.Module,
                entry.Meter,
                entry.Quantity.ToString(CultureInfo.InvariantCulture),
            ]);
        }

        return csv.ToString();
    }

    private static void AppendRow(StringBuilder csv, IReadOnlyList<string> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                csv.Append(Separator);
            }

            csv.Append(Escape(fields[i]));
        }

        csv.Append(LineBreak);
    }

    private static string Escape(string field)
    {
        return field.IndexOfAny(NeedsQuoting) < 0
            ? field
            : Quote + field.Replace(Quote.ToString(), EscapedQuote, StringComparison.Ordinal) + Quote;
    }
}
