// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.RuleSets;
using Homeport.Sdk;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Api;

/// <summary>The problem types the e-invoicing module answers with, under a prefix of its own.</summary>
internal static class EInvoicingProblems
{
    private const string TypePrefix = "https://homeport.data-worx.de/modules/e-invoicing/problems/";

    /// <summary>The document cannot be read as what its content type declares: well-formed XML or a PDF.</summary>
    internal static readonly ProblemType UnreadableDocument = new(
        new Uri(TypePrefix + "unreadable-document"),
        "The document cannot be read as well-formed XML or as a PDF.",
        StatusCodes.Status422UnprocessableEntity);

    /// <summary>The document is well-formed XML, but its root element names no invoice syntax.</summary>
    internal static readonly ProblemType UnknownSyntax = new(
        new Uri(TypePrefix + "unknown-syntax"),
        "The document is not written in an invoice syntax the module knows.",
        StatusCodes.Status422UnprocessableEntity);

    /// <summary>The document is a PDF, but carries no invoice under a file name the module knows.</summary>
    internal static readonly ProblemType PdfWithoutInvoice = new(
        new Uri(TypePrefix + "pdf-without-invoice"),
        "The PDF carries no embedded invoice the module knows.",
        StatusCodes.Status422UnprocessableEntity);

    /// <summary>The invoice claims no rule set the module knows, nor a specification of EN 16931.</summary>
    internal static readonly ProblemType UnknownRuleSet = new(
        new Uri(TypePrefix + "unknown-rule-set"),
        "The invoice claims no rule set the module knows.",
        StatusCodes.Status422UnprocessableEntity);

    /// <summary>The problem type a business error of the module is answered with.</summary>
    internal static ProblemType For(Error error) => error.Code switch
    {
        DocumentRules.UnreadableCode => UnreadableDocument,
        DocumentRules.UnknownSyntaxCode => UnknownSyntax,
        DocumentRules.PdfWithoutInvoiceCode => PdfWithoutInvoice,
        RuleSetRules.UnknownRuleSetCode => UnknownRuleSet,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Code, "The module maps every error it returns."),
    };
}
