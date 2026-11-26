// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The XPath 2.0 arithmetic the calculation and decimal rules of EN 16931
/// rely on: amounts cast to xs:decimal, rounded with <c>round</c>, and
/// compared the way the schematron compares them.
/// </summary>
/// <remarks>
/// A missing value is an empty sequence in XPath: arithmetic on it is empty
/// again, and a comparison with it is false. A <see langword="null"/> stands
/// for it here, and every operation keeps that meaning — unlike C#, where
/// <c>null == null</c> holds. An amount beyond the range of
/// <see cref="decimal"/>, which XPath would still compute, counts as missing.
/// </remarks>
internal static class XPathNumbers
{
    // The four characters XPath counts as whitespace; .NET counts more.
    private static readonly char[] XPathWhitespace = [' ', '\t', '\r', '\n'];

    /// <summary>The value of the element cast to xs:decimal; none for no element or a value that is no decimal.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static decimal? Decimal(XElement? element) =>
        element is not null && decimal.TryParse(element.Value.Trim(XPathWhitespace), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    /// <summary>The sum of the elements cast to xs:decimal, as <c>sum</c> returns it: 0 for none.</summary>
    /// <param name="elements">The elements.</param>
    public static decimal? Sum(IEnumerable<XElement> elements) =>
        elements.Select(Decimal).Aggregate((decimal?)0m, (sum, value) => value is null ? sum : Add(sum, value));

    /// <summary>The value rounded to a whole number as XPath's <c>round</c> rounds it: halves towards positive infinity.</summary>
    /// <param name="value">The value.</param>
    public static decimal? Round(decimal? value) => Compute(value, 0.5m, (number, half) => Math.Floor(number + half));

    /// <summary>The value rounded to two decimals as the rules write it, <c>round(x * 10 * 10) div 100</c>.</summary>
    /// <param name="value">The value.</param>
    public static decimal? RoundToCents(decimal? value) => Hundredth(Round(Multiply(value, 100m)));

    /// <summary>The sum of two values.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    public static decimal? Add(decimal? first, decimal? second) => Compute(first, second, (a, b) => a + b);

    /// <summary>The difference of two values.</summary>
    /// <param name="first">The value subtracted from.</param>
    /// <param name="second">The value subtracted.</param>
    public static decimal? Subtract(decimal? first, decimal? second) => Compute(first, second, (a, b) => a - b);

    /// <summary>The product of two values.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    public static decimal? Multiply(decimal? first, decimal? second) => Compute(first, second, (a, b) => a * b);

    /// <summary>Whether both values exist and are equal, as <c>=</c> compares two decimals.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    public static bool AreEqual(decimal? first, decimal? second) => first is { } a && second is { } b && a == b;

    /// <summary>Whether both values exist and the first is less than the second, as <c>&lt;</c> compares two decimals.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    public static bool IsLess(decimal? first, decimal? second) => first is { } a && second is { } b && a < b;

    /// <summary>Whether both values exist and the first is at most the second, as <c>&lt;=</c> compares two decimals.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    public static bool IsLessOrEqual(decimal? first, decimal? second) => first is { } a && second is { } b && a <= b;

    /// <summary>The absolute value, as <c>abs</c> returns it.</summary>
    /// <param name="value">The value.</param>
    public static decimal? Abs(decimal? value) => value is { } number ? Math.Abs(number) : null;

    /// <summary>The value divided by 100, as <c>x div 100</c> divides a decimal, which is exact.</summary>
    /// <param name="value">The value.</param>
    public static decimal? Hundredth(decimal? value) => value / 100m;

    /// <summary>Whether any of the elements equals <paramref name="value"/>, as <c>=</c> compares untyped text with a decimal: both as xs:double.</summary>
    /// <param name="elements">The elements; none makes the comparison false.</param>
    /// <param name="value">The decimal compared with.</param>
    public static bool AnyEqualsAsDouble(IEnumerable<XElement> elements, decimal? value) =>
        value is { } number
        && elements.Any(element => double.TryParse(element.Value.Trim(XPathWhitespace), NumberStyles.Float, CultureInfo.InvariantCulture, out var text)
            && text == (double)number);

    /// <summary>The number of characters after the first point of the element's text, as <c>string-length(substring-after(x, '.'))</c> counts them: 0 for no element.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static int FractionLength(XElement? element)
    {
        var text = element?.Value ?? string.Empty;
        var point = text.IndexOf('.', StringComparison.Ordinal);

        return point < 0 ? 0 : RuleXml.StringLength(text[(point + 1)..]);
    }

    /// <summary>The result of <paramref name="operation"/> on two values; none when one is missing or the result leaves the range of <see cref="decimal"/>.</summary>
    private static decimal? Compute(decimal? first, decimal? second, Func<decimal, decimal, decimal> operation)
    {
        if (first is not { } a || second is not { } b)
        {
            return null;
        }

        try
        {
            return operation(a, b);
        }
        catch (OverflowException)
        {
            // XPath's xs:decimal has no upper bound; beyond decimal's range the value counts as missing.
            return null;
        }
    }
}
