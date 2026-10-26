// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Aspire.AppHost;

/// <summary>
/// The topology of Homeport and its single source of truth. Run it to start
/// everything locally; publish it to regenerate <c>deploy/compose/</c>.
/// </summary>
public static class Program
{
    /// <summary>Declares every resource and runs or publishes them.</summary>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // The compose output of `--operation publish`. No dashboard in it: its
        // image cannot be pinned by digest.
        builder.AddDockerComposeEnvironment("compose")
            .WithDashboard(false);

        // Third-party images are pinned by digest. The tag stays next to it, so
        // a reader sees which version the digest stands for.
        builder.AddPostgres("postgres")
            .WithImageTag("18.3")
            .WithImageSHA256("7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db");

        builder.AddRabbitMQ("rabbitmq")
            .WithImageTag("4.3")
            .WithImageSHA256("8144dac709541439da6106d7421079132171b7f0a48b7a7b1f4bd39b22abdb75");

        builder.AddValkey("valkey")
            .WithImageTag("9.1")
            .WithImageSHA256("418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd");

        // Development and tests only. An operator brings their own identity
        // provider (ADR-0006), so the compose file runs Keycloak in development
        // mode as the AppHost does, rather than in production mode without TLS.
        builder.AddKeycloak("keycloak", port: 8080)
            .WithImageTag("26.6")
            .WithImageSHA256("0aae0de7fca85525f727d3354df17896092de8bb26ae4c12d89c77e5df8cbce4")
            .PublishAsDockerComposeService((_, service) => service.Command = ["start-dev", "--import-realm"]);

        builder.Build().Run();
    }
}
