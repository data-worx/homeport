// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// The instances of the KoSIT XRechnung test suite (Apache-2.0), embedded
/// unchanged from <c>EInvoicing/TestSuite/</c>, which mirrors the suite's
/// <c>instances/</c> directory. Upstream publishes every instance as a valid
/// XRechnung, so each one's expected verdict is valid.
/// </summary>
/// <remarks>
/// The version is the one <c>src/Modules/EInvoicing/standards.json</c> names
/// for the test suite. An update replaces the directory as a whole.
/// </remarks>
public static class TestSuiteInvoices
{
    private const string ResourcePrefix = "Homeport.Testing.EInvoicing.TestSuite.";

    /// <summary>The path of every instance below <c>TestSuite/</c>, such as <c>standard/01.01a-INVOICE_ubl.xml</c>.</summary>
    public static IReadOnlyList<string> Names { get; } = typeof(TestSuiteInvoices).Assembly
        .GetManifestResourceNames()
        .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
        .Select(name => name[ResourcePrefix.Length..])
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>The instance named <paramref name="name"/>, byte for byte.</summary>
    public static byte[] Bytes(string name)
    {
        using var stream = typeof(TestSuiteInvoices).Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException($"Homeport.Testing carries no test suite instance '{name}'.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
