// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Homeport.Testing.Gateway;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration;

/// <summary>
/// The e-invoicing module in memory, accepting the module tokens of a stand-in
/// for the gateway, as it accepts only the real gateway's tokens when deployed.
/// </summary>
public sealed class EInvoicingBehindGateway : IAsyncLifetime
{
    internal const string Audience = "e-invoicing";

    internal const string ValidateScope = "e-invoicing.validate";

    internal const string ReadScope = "e-invoicing.read";

    private StandInGateway? gateway;
    private WebApplicationFactory<Program>? module;

    private StandInGateway Gateway => gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    private WebApplicationFactory<Program> Started => module ?? throw new InvalidOperationException("The module has not been started.");

    /// <summary>A client of the module with a module token granting <paramref name="scopes"/>.</summary>
    internal HttpClient CreateClient(string scopes = ValidateScope)
    {
        var client = Started.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Gateway.Issue(Audience, scopes));

        return client;
    }

    public async Task InitializeAsync()
    {
        gateway = await StandInGateway.StartAsync();
        module = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host.UseSetting("GatewayTokens:JwksUri", gateway.JwksUri));
    }

    public async Task DisposeAsync()
    {
        if (module is not null)
        {
            await module.DisposeAsync();
        }

        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }
    }
}
