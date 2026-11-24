// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Findings;

/// <summary>How much a finding weighs.</summary>
public enum FindingSeverity
{
    /// <summary>The document breaks a rule; it is no valid e-invoice.</summary>
    Fatal,

    /// <summary>Worth knowing; the document is processed as it is.</summary>
    Warning,

    /// <summary>A note on the check itself, such as findings left unreported.</summary>
    Information,
}
