// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration.Controllers;

/// <summary>
/// The rules the KoSIT validator fires on an instance of the test suite that
/// the module does not implement yet. The comparison with KoSIT leaves them
/// out. The list only shrinks: the ticket that implements a rule deletes its
/// line, and the comparison fails while a listed rule is either fired by the
/// module or no longer fired by KoSIT on any instance.
/// </summary>
internal static class NotYetImplementedRules
{
    internal static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        // #278 — BR-CL
        "BR-CL-10",
        "BR-CL-13",
        "BR-CL-21",

        // #280 — XRechnung
        "BR-DE-TMP-32",

        // #281 — UBL syntax
        "UBL-CR-470",
        "UBL-CR-646",

        // #282 — CII syntax
        "CII-SR-475",
        "CII-SR-476",
    };
}
