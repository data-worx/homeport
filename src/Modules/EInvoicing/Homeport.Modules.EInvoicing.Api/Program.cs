// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Modules.EInvoicing.Domain.RuleSets;
using Homeport.Sdk;

namespace Homeport.Modules.EInvoicing.Api;

/// <summary>
/// The e-invoicing module: validates and reads the electronic invoices an
/// operator receives.
/// </summary>
public sealed class Program
{
    /// <summary>Not static, so the integration tests can name it as their entry point.</summary>
    private Program()
    {
    }

    /// <summary>Wires the module and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddHomeportModule(EInvoicingManifest.Create());

        // Here rather than in the SDK, so the XML docs of the controllers reach
        // the OpenAPI document.
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddSingleton(RuleSetCatalog.LoadEmbedded());
        builder.Services.AddSingleton<DocumentInspection>();

        var app = builder.Build();
        app.MapHomeportModule();
        app.MapControllers();

        app.Run();
    }
}
