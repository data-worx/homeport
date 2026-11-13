// SPDX-License-Identifier: AGPL-3.0-or-later

using Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.Hosting;

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

    // Aspire's own initdb arguments, plus ICU as the collation provider: the
    // Alpine image's C library sorts text by byte, ICU sorts it as the Debian
    // image did.
    private const string InitDbArgs = "--auth-host=scram-sha-256 --auth-local=scram-sha-256 --locale-provider=icu --icu-locale=und";

    // Relative to the AppHost project: the scripts and the realm live with the deployment.
    private const string InitFiles = "../../../deploy/postgres/init";
    private const string RabbitMqInitFiles = "../../../deploy/rabbitmq/init";
    private const string Realms = "../../../deploy/keycloak/realms";

    // The realm in deploy/keycloak/realms/ and the audience its clients' tokens carry.
    private const string Realm = "homeport";
    private const string Audience = "homeport";

    // The audience of the operator's tokens, which the gateway's admin API accepts (ADR-0020).
    private const string AdminAudience = "homeport-portal";

    // The gateway's second endpoint, which serves the admin API only.
    private const string GatewayAdminEndpoint = "admin";

    // Keycloak's port on the host, fixed so its issuer is the same for a
    // client on the host and for the gateway on the compose network.
    private const int KeycloakPort = 8080;

    // The portal's client in the realm, and the portal's HTTPS port on the
    // host in a local run, fixed so the realm can name the addresses the
    // identity provider may send the operator back to.
    private const string PortalClientId = "portal";
    private const int PortalPort = 7400;
    private const string PortalEndpoint = "https";

    // Where the broker's init script is mounted, which starts it with a user per service.
    private const string RabbitMqInitDirectory = "/homeport-init";

    // Where the gateway serves the consolidated API docs, and the link the dashboard shows for them.
    private const string ApiDocsPath = "/platform/docs";
    private const string ApiDocsDisplayText = "API docs";

    // Two, so every run shows that a tenant's rate limit holds across
    // replicas: they share its bucket in Valkey (ADR-0007).
    private const int GatewayReplicas = 2;

    /// <summary>Declares every resource and runs or publishes them.</summary>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // The compose output of `--operation publish`. No dashboard in it: its
        // image cannot be pinned by digest.
        builder.AddDockerComposeEnvironment("compose")
            .WithDashboard(false);

        // Third-party images are pinned by digest, written as tag@digest so the
        // compose file carries both: a reader sees which version the digest
        // stands for, and Dependabot proposes a new digest within that tag
        // rather than the digest of `latest`. Its pull request changes only
        // deploy/compose/; the new digest is copied here in the same request.
        // Alpine variants where they exist: the Debian ones carry vulnerable
        // packages that have no fix yet, and the scan of every image fails on
        // them (ADR-0009).
        // One database for all of Homeport (ADR-0005). The init script gives each
        // service a schema, an owner role for its migrations and an app role to
        // run as; the passwords are generated like the server's own.
        var controlPlaneOwnerPassword = builder.AddParameter("control-plane-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var controlPlaneAppPassword = builder.AddParameter("control-plane-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringOwnerPassword = builder.AddParameter("metering-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringAppPassword = builder.AddParameter("metering-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);

        var postgres = builder.AddPostgres("postgres")
            .WithImageTag("18.6-alpine@sha256:123b17998ccaf47e53b502e8c6a1e79afd8f579c6f3eec83c563da026f957395")
            .WithEnvironment("POSTGRES_INITDB_ARGS", InitDbArgs)
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
            .WithImageTag("4.3-alpine@sha256:68fd0566c1c5a5aef2d7b3ad55af8be503a767c1ff8646d7088c8c7946270cf9")
            .WithEnvironment("GATEWAY_RABBITMQ_PASSWORD", gatewayRabbitMqPassword)
            .WithEnvironment("METERING_RABBITMQ_PASSWORD", meteringRabbitMqPassword)
            .WithBindMount(RabbitMqInitFiles, RabbitMqInitDirectory, isReadOnly: true)
            .WithEntrypoint("bash")
            .WithArgs($"{RabbitMqInitDirectory}/init-homeport.sh", "rabbitmq-server");
        var rabbitMqEndpoint = rabbitMq.Resource.PrimaryEndpoint;

        var valkey = builder.AddValkey("valkey")
            .WithImageTag("9.1-alpine@sha256:bf09d58e96d6b4524a15396b65b242bbe18beebbcc15a04d8ee75ddac62fae0b");

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
        var seedClientId = builder.AddParameter("seed-client-id", new LocalDefault("local-client"));

        // The portal's confidential client and a dev operator who holds the
        // admin role (ADR-0020); the operator's password is generated like
        // the clients' secrets. The portal's address is where the browser
        // reaches it, which only the operator's reverse proxy knows once the
        // compose file runs.
        var portalClientSecret = builder.AddParameter("portal-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var portalAddress = builder.AddParameter("portal-address", new LocalDefault($"{Uri.UriSchemeHttps}://localhost:{PortalPort}"));
        var keycloakHostAddress = $"{Uri.UriSchemeHttp}://localhost:{KeycloakPort}";
        var keycloak = builder.AddKeycloak("keycloak", port: KeycloakPort)
            .WithImageTag("26.8@sha256:c68e286cacd87b942ac1939a0a1481461109d6acd3a57dc496969a319a76bbf0")
            .WithRealmImport(Realms)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_ID", seedClientId)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_SECRET", builder.AddParameter("local-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_UNMAPPED_CLIENT_SECRET", builder.AddParameter("unmapped-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_AUDIENCE", Audience)
            .WithEnvironment("HOMEPORT_ADMIN_AUDIENCE", AdminAudience)
            .WithEnvironment("HOMEPORT_PORTAL_CLIENT_ID", PortalClientId)
            .WithEnvironment("HOMEPORT_PORTAL_CLIENT_SECRET", portalClientSecret)
            .WithEnvironment("HOMEPORT_PORTAL_ADDRESS", portalAddress)
            .WithEnvironment("HOMEPORT_OPERATOR_PASSWORD", builder.AddParameter("operator-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
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

            // The consolidated API docs, which the gateway serves in a local
            // run (ADR-0017), one click away in the dashboard.
            .WithUrlForEndpoint(Uri.UriSchemeHttp, endpoint => new ResourceUrlAnnotation
            {
                Url = endpoint.Url + ApiDocsPath,
                DisplayText = ApiDocsDisplayText,
            })
            .WithReference(registry)
            .WithEnvironment("ConnectionStrings__rabbitmq", BrokerConnectionString(rabbitMqEndpoint, "gateway", gatewayRabbitMqPassword))
            .WaitFor(rabbitMq)
            .WithReference(valkey)
            .WaitFor(valkey)
            .WithReplicas(GatewayReplicas)

            // The admin API's own endpoint, added after the external ones, so
            // it stays internal: the compose file exposes it on the network
            // and publishes no port for it (ADR-0020).
            .WithHttpEndpoint(name: GatewayAdminEndpoint)

            // The compose publisher does not take the replicas from the model.
            .PublishAsDockerComposeService((_, service) => service.Deploy = new Deploy { Replicas = GatewayReplicas });

        // The gateway tells the admin endpoint apart by the port a request
        // arrives on, and accepts operator tokens from the same realm.
        gateway
            .WithEnvironment("AdminApi__Port", gateway.GetEndpoint(GatewayAdminEndpoint).Property(EndpointProperty.TargetPort))
            .WithEnvironment("AdminApi__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint(Uri.UriSchemeHttp)}/realms/{Realm}"))
            .WithEnvironment("AdminApi__Audience", AdminAudience)
            .WithEnvironment("AdminApi__RequireHttpsMetadata", bool.FalseString);

        // The registry validates the gateway's service tokens against its JWKS,
        // and Echo its module tokens.
        registry.WithReference(gateway);
        echo.WithReference(gateway);

        // The control plane: `migrate` applies its migrations as the owner role
        // and seeds one tenant, plan and client, whose values configuration
        // overrides; the service starts after it completed and runs as the app
        // role.
        var controlPlaneMigrate = builder.AddProject<Projects.Homeport_Platform_ControlPlane_Api>("control-plane-migrate", launchProfileName: null)
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane-migrate")
            .WithEnvironment("ConnectionStrings__control-plane-owner", ConnectionString(postgresEndpoint, "control_plane_owner", controlPlaneOwnerPassword))
            .WithEnvironment("Seed__TenantId", builder.AddParameter("seed-tenant-id", new LocalDefault("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61")))
            .WithEnvironment("Seed__TenantName", builder.AddParameter("seed-tenant-name", new LocalDefault("Local tenant")))
            .WithEnvironment("Seed__PlanId", builder.AddParameter("seed-plan-id", new LocalDefault("8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50")))
            .WithEnvironment("Seed__PlanName", builder.AddParameter("seed-plan-name", new LocalDefault("Local plan")))
            .WithEnvironment("Seed__RequestsPerSecond", builder.AddParameter("seed-plan-requests-per-second", new LocalDefault("10")))
            .WithEnvironment("Seed__Burst", builder.AddParameter("seed-plan-burst", new LocalDefault("20")))
            .WithEnvironment("Seed__ClientId", seedClientId)
            .WaitFor(postgres);

        var controlPlane = builder.AddProject<Projects.Homeport_Platform_ControlPlane_Api>("control-plane")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane")
            .WithEnvironment("ConnectionStrings__control-plane", ConnectionString(postgresEndpoint, "control_plane_app", controlPlaneAppPassword))
            .WithReference(gateway)
            .WaitForCompletion(controlPlaneMigrate)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // The gateway looks clients up at the control plane, and forwards the
        // admin API of tenants to it, which YARP needs the endpoint itself for.
        gateway.WithReference(controlPlane);
        gateway.WithEnvironment("PlatformRoutes__ControlPlaneUrl", controlPlane.GetEndpoint(Uri.UriSchemeHttp));

        // Metering: `migrate` applies its migrations as the owner role; the
        // service starts after it completed, runs as the app role and
        // consumes the gateway's usage events with a broker user of its own.
        // It serves the usage export the gateway forwards, with caller tokens
        // it validates against the gateway's JWKS (ADR-0019).
        var meteringMigrate = builder.AddProject<Projects.Homeport_Platform_Metering_Api>("metering-migrate", launchProfileName: null)
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering-migrate")
            .WithEnvironment("ConnectionStrings__metering-owner", ConnectionString(postgresEndpoint, "metering_owner", meteringOwnerPassword))
            .WaitFor(postgres);

        var metering = builder.AddProject<Projects.Homeport_Platform_Metering_Api>("metering")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering")
            .WithEnvironment("ConnectionStrings__metering", ConnectionString(postgresEndpoint, "metering_app", meteringAppPassword))
            .WithEnvironment("ConnectionStrings__rabbitmq", BrokerConnectionString(rabbitMqEndpoint, "metering", meteringRabbitMqPassword))
            .WithReference(gateway)
            .WaitForCompletion(meteringMigrate)
            .WaitFor(rabbitMq)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // The gateway forwards /platform/v1/usage to metering. YARP takes a
        // destination address as it is, so it gets the endpoint itself rather
        // than a service name to discover.
        gateway.WithEnvironment("PlatformRoutes__MeteringUrl", metering.GetEndpoint(Uri.UriSchemeHttp));

        // The admin portal (ADR-0020). The operator signs in at Keycloak,
        // whose metadata the portal fetches over the network it shares with
        // it, like the gateway. A local run serves it over HTTPS with the
        // developer certificate. The compose file publishes plain HTTP: TLS
        // ends at the operator's reverse proxy, whose forwarded headers the
        // portal applies only once the operator trusts it.
        var portal = builder.AddProject<Projects.Homeport_Platform_Portal>("portal", launchProfileName: null)
            .DisableForwardedHeaders()
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-portal")
            .WithEnvironment("SignIn__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint(Uri.UriSchemeHttp)}/realms/{Realm}"))
            .WithEnvironment("SignIn__ClientId", PortalClientId)
            .WithEnvironment("SignIn__ClientSecret", portalClientSecret)
            .WithEnvironment("SignIn__RequireHttpsMetadata", bool.FalseString)
            .WaitFor(keycloak);
        if (builder.ExecutionContext.IsRunMode)
        {
            // Development serves the static files from the build output, as
            // a launch profile would; the portal has none here, so the
            // endpoint is the AppHost's alone.
            portal.WithHttpsEndpoint(port: PortalPort, name: PortalEndpoint)
                .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath, endpointName: PortalEndpoint)
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environments.Development);
        }
        else
        {
            portal.WithHttpEndpoint()
                .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);
        }

        portal.WithExternalHttpEndpoints();

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
