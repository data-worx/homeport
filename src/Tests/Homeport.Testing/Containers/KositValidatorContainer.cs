// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Xml.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Homeport.Testing.Containers;

/// <summary>
/// The KoSIT validator with the XRechnung configuration in its daemon mode,
/// the reference the e-invoicing module is compared with. The image is built
/// from <c>Containers/KositValidator/Dockerfile</c> in the checkout, which
/// downloads the validator and its configuration; it is a test tool, never
/// shipped. Needs a running Docker and, on the first build, the network.
/// </summary>
public sealed class KositValidatorContainer : IAsyncLifetime
{
    // The tag names the versions the Dockerfile downloads, so changing them
    // builds a new image instead of reusing a stale one.
    private const string ImageName = "homeport-test-kosit-validator:1.6.3-v2026-08-31";
    private const ushort Port = 8080;

    private static readonly XNamespace Report = "http://www.xoev.de/de/validator/varl/1";

    private readonly IFutureDockerImage image = new ImageFromDockerfileBuilder()
        .WithDockerfileDirectory(Path.Combine(RepositoryRoot.Find(), "src", "Tests", "Homeport.Testing", "Containers", "KositValidator"))
        .WithName(ImageName)
        .WithCleanUp(false)
        .WithDeleteIfExists(false)
        .Build();

    private IContainer? container;
    private HttpClient? client;

    private HttpClient Client => client ?? throw new InvalidOperationException("The KoSIT validator has not been started.");

    /// <summary>Validates <paramref name="document"/> and returns what the validator's report says.</summary>
    public async Task<KositReport> ValidateAsync(byte[] document)
    {
        using var content = new ByteArrayContent(document);
        content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Xml);
        using var response = await Client.PostAsync(new Uri("/", UriKind.Relative), content);
        var report = XDocument.Parse(await response.Content.ReadAsStringAsync());

        var accepted = report.Root?.Element(Report + "assessment")?.Element(Report + "accept") is not null;
        var rules = report.Descendants(Report + "message")
            .Select(message => (string?)message.Attribute("code"))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        return new KositReport(accepted, rules);
    }

    public async Task InitializeAsync()
    {
        await image.CreateAsync();
        container = new ContainerBuilder(image)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(Port).ForPath("/")))
            .Build();
        await container.StartAsync();
        client = new HttpClient { BaseAddress = new UriBuilder(Uri.UriSchemeHttp, container.Hostname, container.GetMappedPublicPort(Port)).Uri };
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (container is not null)
        {
            await container.StopAsync();
            await container.DisposeAsync();
        }

        await image.DisposeAsync();
    }
}

/// <summary>What the KoSIT validator's report says about a document.</summary>
/// <param name="Accepted">Whether the assessment accepts the document.</param>
/// <param name="Rules">The rule IDs of every message the report carries, whatever its level.</param>
public sealed record KositReport(bool Accepted, IReadOnlySet<string> Rules);
