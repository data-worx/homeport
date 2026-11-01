// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.ServiceDefaults;

namespace Homeport.Aspire.AppHost;

/// <summary>
/// The topology of Homeport and its single source of truth. Run it to start
/// everything locally; publish it to regenerate <c>deploy/compose/</c>.
/// </summary>
public static class Program
{
    private const string Database = "homeport";

    // Long enough without special characters, which a connection string would
    // have to escape.
    private const int PasswordLength = 32;

    // Relative to the AppHost project: the script lives with the deployment.
    private const string InitFiles = "../../../deploy/postgres/init";

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
        // One database for all of Homeport (ADR-0005). The init script gives each
        // service a schema, an owner role for its migrations and an app role to
        // run as; the passwords are generated like the server's own.
        var controlPlaneOwnerPassword = builder.AddParameter("control-plane-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var controlPlaneAppPassword = builder.AddParameter("control-plane-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);

        var postgres = builder.AddPostgres("postgres")
            .WithImageTag("18.3")
            .WithImageSHA256("7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db")
            .WithEnvironment("POSTGRES_DB", Database)
            .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", controlPlaneOwnerPassword)
            .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", controlPlaneAppPassword)
            .WithInitFiles(InitFiles);
        var postgresEndpoint = postgres.Resource.PrimaryEndpoint;

        builder.AddRabbitMQ("rabbitmq")
            .WithImageTag("4.3")
            .WithImageSHA256("8144dac709541439da6106d7421079132171b7f0a48b7a7b1f4bd39b22abdb75");

        var valkey = builder.AddValkey("valkey")
            .WithImageTag("9.1")
            .WithImageSHA256("418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd");

        // Development and tests only. An operator brings their own identity
        // provider (ADR-0006), so the compose file runs Keycloak in development
        // mode as the AppHost does, rather than in production mode without TLS.
        builder.AddKeycloak("keycloak", port: 8080)
            .WithImageTag("26.6")
            .WithImageSHA256("0aae0de7fca85525f727d3354df17896092de8bb26ae4c12d89c77e5df8cbce4")
            .PublishAsDockerComposeService((_, service) => service.Command = ["start-dev", "--import-realm"]);

        // A registration key per module id (ADR-0012). A local run generates
        // one and keeps it in the user secrets; the compose file takes it from
        // the environment.
        var echoRegistrationKey = builder.AddParameter("echo-registration-key", new GeneratedRegistrationKey(), secret: true, persist: true);

        // Modules register here and the gateway reads their routes from it.
        var registry = builder.AddProject<Projects.Homeport_Platform_Registry_Api>("registry")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-registry")
            .WithEnvironment("Registry__RegistrationKeys__echo__0", echoRegistrationKey)
            .WithReference(valkey)
            .WaitFor(valkey)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // Modules. The resource is named after the module id; service.name
        // follows the observability rule instead. Each instance registers at
        // the registry with the address the gateway reaches it at.
        var echo = builder.AddProject<Projects.Homeport_Modules_Echo_Api>("echo")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-module-echo")
            .WithEnvironment("Homeport__Registry__Url", $"{Uri.UriSchemeHttp}://registry")
            .WithEnvironment("Homeport__Registry__Key", echoRegistrationKey)
            .WithReference(registry)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);
        echo.WithEnvironment("Homeport__Registry__InstanceAddress", echo.GetEndpoint(Uri.UriSchemeHttp));

        // The gateway's signing key. A local run generates one and keeps it in
        // the user secrets; the compose file takes it from the environment. The
        // additional public key is empty until a rotation needs it.
        var signingKey = builder.AddParameter("gateway-signing-key", new GeneratedSigningKey(), secret: true, persist: true);
        var signingKeyId = builder.AddParameter("gateway-signing-key-id", "local");
        var additionalPublicKey = builder.AddParameter("gateway-additional-public-key", string.Empty);
        var additionalKeyId = builder.AddParameter("gateway-additional-key-id", string.Empty);

        // The one service with an external endpoint: modules are reachable only
        // through it, on the routes it reads from the registry.
        var gateway = builder.AddProject<Projects.Homeport_Platform_Gateway_Api>("gateway")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-gateway")
            .WithEnvironment("Signing__Key", signingKey)
            .WithEnvironment("Signing__KeyId", signingKeyId)
            .WithEnvironment("Signing__AdditionalPublicKey", additionalPublicKey)
            .WithEnvironment("Signing__AdditionalKeyId", additionalKeyId)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath)
            .WithExternalHttpEndpoints()
            .WithReference(registry);

        // The registry validates the gateway's service tokens against its JWKS.
        registry.WithReference(gateway);

        // The control plane: `migrate` applies its migrations as the owner role
        // and seeds one tenant, plan and client; the service starts after it
        // completed and runs as the app role.
        var controlPlaneMigrate = builder.AddProject<Projects.Homeport_Platform_ControlPlane>("control-plane-migrate")
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane-migrate")
            .WithEnvironment("ConnectionStrings__control-plane-owner", ConnectionString(postgresEndpoint, "control_plane_owner", controlPlaneOwnerPassword))
            .WithEnvironment("Seed__TenantId", builder.AddParameter("seed-tenant-id", "3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"))
            .WithEnvironment("Seed__TenantName", builder.AddParameter("seed-tenant-name", "Local tenant"))
            .WithEnvironment("Seed__PlanId", builder.AddParameter("seed-plan-id", "8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50"))
            .WithEnvironment("Seed__PlanName", builder.AddParameter("seed-plan-name", "Local plan"))
            .WithEnvironment("Seed__RequestsPerSecond", builder.AddParameter("seed-plan-requests-per-second", "10"))
            .WithEnvironment("Seed__Burst", builder.AddParameter("seed-plan-burst", "20"))
            .WithEnvironment("Seed__ClientId", builder.AddParameter("seed-client-id", "local-client"))
            .WaitFor(postgres);

        builder.AddProject<Projects.Homeport_Platform_ControlPlane>("control-plane")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane")
            .WithEnvironment("ConnectionStrings__control-plane", ConnectionString(postgresEndpoint, "control_plane_app", controlPlaneAppPassword))
            .WithReference(gateway)
            .WaitForCompletion(controlPlaneMigrate)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        builder.Build().Run();
    }

    // A role's connection to the Homeport database, built from the server's
    // endpoint, so it resolves locally and in the compose file alike.
    private static ReferenceExpression ConnectionString(EndpointReference postgres, string role, IResourceBuilder<ParameterResource> password)
    {
        return ReferenceExpression.Create(
            $"Host={postgres.Property(EndpointProperty.Host)};Port={postgres.Property(EndpointProperty.Port)};Database={Database};Username={role};Password={password}");
    }
}
