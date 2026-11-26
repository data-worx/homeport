// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// The instances of the KoSIT test suite on which the module reaches another
/// verdict than the KoSIT validator, each for a reason a ticket removes. The
/// list only shrinks: the tests expect the other verdict on a listed
/// instance, so they fail once the module and KoSIT agree, and the ticket
/// that made them agree deletes the line.
/// </summary>
public static class KnownVerdictDifferences
{
    /// <summary>The names of the instances, as <see cref="TestSuiteInvoices.Names"/> spells them.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        // The module knows no rule set XRechnung CVD and checks the invoice
        // against EN 16931 alone, where BR-CL-13 is fatal; KoSIT's scenario
        // for CVD weighs it as information and accepts the invoice.
        "technical-cases/cvd/02.01a-cvd_INVOICE_ubl.xml",
        "technical-cases/cvd/02.01a-cvd_INVOICE_uncefact.xml",
    };
}
