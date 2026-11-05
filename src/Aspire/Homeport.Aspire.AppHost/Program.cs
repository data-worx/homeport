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

    // Relative to the AppHost project: the scripts and the realm live with the deployment.
    private const string InitFiles = "../../../deploy/postgres/init";
    private const string RabbitMqInitFiles = "../../../deploy/rabbitmq/init";
    private const string Realms = "../../../deploy/keycloak/realms";

    // The realm in deploy/keycloak/realms/ and the audience its clients' tokens carry.
    private const string Realm = "homeport";
    private const string Audience = "homeport";

    // Keycloak's port on the host, fixed so its issuer is the same for a
    // client on the host and for the gateway on the compose network.
    private const int KeycloakPort = 8080;

    // Where the broker's init script is mounted, which starts it with a user per service.
    private const string RabbitMqInitDirectory = "/homeport-init";

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
        var meteringOwnerPassword = builder.AddParameter("metering-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringAppPassword = builder.AddParameter("metering-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);

        var postgres = builder.AddPostgres("postgres")
            .WithImageTag("18.3")
            .WithImageSHA256("7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db")
            .WithEnvironment("POSTGRES_DB", Database)
            .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", controlPlaneOwnerPassword)
            .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", controlPlaneAppPassword)
            .WithEnvironment("METERING_OWNER_PASSWORD", meteringOwnerPassword)
            .WithEnvironment("METERING_APP_PASSWORD", meteringAppPassword)
            .WithInitFiles(InitFiles);
        var postgresEndpoint = postgres.Resource.PrimaryEndpoint;

        // One broker; its init script gives each service a user of its own
        // (ADR-0014), with a password generated like the database's.
        var gatewayRabbitMqPassword = builder.AddParameter("gateway-rabbitmq-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringRabbitMqPassword = builder.AddParameter("metering-rabbitmq-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var rabbitMq = builder.AddRabbitMQ("rabbitmq")
            .WithImageTag("4.3")
            .WithImageSHA256("8144dac709541439da6106d7421079132171b7f0a48b7a7b1f4bd39b22abdb75")
            .WithEnvironment("GATEWAY_RABBITMQ_PASSWORD", gatewayRabbitMqPassword)
            .WithEnvironment("METERING_RABBITMQ_PASSWORD", meteringRabbitMqPassword)
            .WithBindMount(RabbitMqInitFiles, RabbitMqInitDirectory, isReadOnly: true)
            .WithEntrypoint("bash")
            .WithArgs($"{RabbitMqInitDirectory}/init-homeport.sh", "rabbitmq-server");
        var rabbitMqEndpoint = rabbitMq.Resource.PrimaryEndpoint;

        var valkey = builder.AddValkey("valkey")
            .WithImageTag("9.1")
            .WithImageSHA256("418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd");

        // Development and tests only. An operator brings their own identity
        // provider (ADR-0006), so the compose file runs Keycloak in development
        // mode as the AppHost does, rather than in production mode without TLS.
        // The realm holds the client the control plane seeds and one it does not
        // know; their secrets are generated and never written into the realm file.
        // Keycloak is reachable from the host, where clients ask it for tokens.
        // Its issuer is always the host address, while the gateway fetches the
        // metadata and keys over the network it shares with it. It serves plain
        // HTTP even where the developer certificate is trusted, which would
        // otherwise move it to HTTPS and leave the HTTP address unanswered.
        var seedClientId = builder.AddParameter("seed-client-id", "local-client");
        var keycloakHostAddress = $"{Uri.UriSchemeHttp}://localhost:{KeycloakPort}";
        var keycloak = builder.AddKeycloak("keycloak", port: KeycloakPort)
            .WithImageTag("26.6")
            .WithImageSHA256("0aae0de7fca85525f727d3354df17896092de8bb26ae4c12d89c77e5df8cbce4")
            .WithRealmImport(Realms)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_ID", seedClientId)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_SECRET", builder.AddParameter("local-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_UNMAPPED_CLIENT_SECRET", builder.AddParameter("unmapped-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_AUDIENCE", Audience)
            .WithEnvironment("KC_HOSTNAME", keycloakHostAddress)
            .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", bool.TrueString.ToLowerInvariant())
            .WithExternalHttpEndpoints()
            .PublishAsDockerComposeService((_, service) => service.Command = ["start-dev", "--import-realm"]);
#pragma warning disable ASPIRECERTIFICATES001 // Experimental in Aspire, and the only way to keep the integration off HTTPS.
        keycloak.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

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
        // through it, on the routes it reads from the registry, for clients with
        // a token from Keycloak that the control plane knows. It applies
        // forwarded headers only from the proxies an operator configures, so
        // it does not take Aspire's publish default of trusting every caller.
        var gateway = builder.AddProject<Projects.Homeport_Platform_Gateway_Api>("gateway")
            .DisableForwardedHeaders()
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-gateway")
            .WithEnvironment("Signing__Key", signingKey)
            .WithEnvironment("Signing__KeyId", signingKeyId)
            .WithEnvironment("Signing__AdditionalPublicKey", additionalPublicKey)
            .WithEnvironment("Signing__AdditionalKeyId", additionalKeyId)
            .WithEnvironment("ClientAuthentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint(Uri.UriSchemeHttp)}/realms/{Realm}"))
            .WithEnvironment("ClientAuthentication__Audience", Audience)
            .WithEnvironment("ClientAuthentication__RequireHttpsMetadata", bool.FalseString)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath)
            .WithExternalHttpEndpoints()
            .WithReference(registry)
            .WithEnvironment("ConnectionStrings__rabbitmq", BrokerConnectionString(rabbitMqEndpoint, "gateway", gatewayRabbitMqPassword))
            .WaitFor(rabbitMq);

        // The registry validates the gateway's service tokens against its JWKS,
        // and Echo its module tokens.
        registry.WithReference(gateway);
        echo.WithReference(gateway);

        // The control plane: `migrate` applies its migrations as the owner role
        // and seeds one tenant, plan and client; the service starts after it
        // completed and runs as the app role.
        var controlPlaneMigrate = builder.AddProject<Projects.Homeport_Platform_ControlPlane>("control-plane-migrate", launchProfileName: null)
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane-migrate")
            .WithEnvironment("ConnectionStrings__control-plane-owner", ConnectionString(postgresEndpoint, "control_plane_owner", controlPlaneOwnerPassword))
            .WithEnvironment("Seed__TenantId", builder.AddParameter("seed-tenant-id", "3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"))
            .WithEnvironment("Seed__TenantName", builder.AddParameter("seed-tenant-name", "Local tenant"))
            .WithEnvironment("Seed__PlanId", builder.AddParameter("seed-plan-id", "8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50"))
            .WithEnvironment("Seed__PlanName", builder.AddParameter("seed-plan-name", "Local plan"))
            .WithEnvironment("Seed__RequestsPerSecond", builder.AddParameter("seed-plan-requests-per-second", "10"))
            .WithEnvironment("Seed__Burst", builder.AddParameter("seed-plan-burst", "20"))
            .WithEnvironment("Seed__ClientId", seedClientId)
            .WaitFor(postgres);

        var controlPlane = builder.AddProject<Projects.Homeport_Platform_ControlPlane>("control-plane")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane")
            .WithEnvironment("ConnectionStrings__control-plane", ConnectionString(postgresEndpoint, "control_plane_app", controlPlaneAppPassword))
            .WithReference(gateway)
            .WaitForCompletion(controlPlaneMigrate)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // The gateway looks clients up at the control plane.
        gateway.WithReference(controlPlane);

        // Metering: `migrate` applies its migrations as the owner role; the
        // service starts after it completed, runs as the app role and
        // consumes the gateway's usage events with a broker user of its own.
        var meteringMigrate = builder.AddProject<Projects.Homeport_Platform_Metering_Api>("metering-migrate", launchProfileName: null)
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering-migrate")
            .WithEnvironment("ConnectionStrings__metering-owner", ConnectionString(postgresEndpoint, "metering_owner", meteringOwnerPassword))
            .WaitFor(postgres);

        builder.AddProject<Projects.Homeport_Platform_Metering_Api>("metering")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering")
            .WithEnvironment("ConnectionStrings__metering", ConnectionString(postgresEndpoint, "metering_app", meteringAppPassword))
            .WithEnvironment("ConnectionStrings__rabbitmq", BrokerConnectionString(rabbitMqEndpoint, "metering", meteringRabbitMqPassword))
            .WaitForCompletion(meteringMigrate)
            .WaitFor(rabbitMq)
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

    // A service's connection to the broker as its own user, built from the
    // broker's endpoint like the database connections.
    private static ReferenceExpression BrokerConnectionString(EndpointReference rabbitMq, string user, IResourceBuilder<ParameterResource> password)
    {
        return ReferenceExpression.Create(
            $"amqp://{user}:{password}@{rabbitMq.Property(EndpointProperty.Host)}:{rabbitMq.Property(EndpointProperty.Port)}");
    }
}
