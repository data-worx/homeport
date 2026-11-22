// SPDX-License-Identifier: AGPL-3.0-or-later

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

    // Relative to the AppHost project: the hosts it runs as resources.
    private const string RegistryProject = "../../Platform/Registry/Homeport.Platform.Registry.Api/Homeport.Platform.Registry.Api.csproj";
    private const string EchoProject = "../../Modules/Echo/Homeport.Modules.Echo.Api/Homeport.Modules.Echo.Api.csproj";
    private const string GatewayProject = "../../Platform/Gateway/Homeport.Platform.Gateway.Api/Homeport.Platform.Gateway.Api.csproj";
    private const string ControlPlaneProject = "../../Platform/ControlPlane/Homeport.Platform.ControlPlane.Api/Homeport.Platform.ControlPlane.Api.csproj";
    private const string MeteringProject = "../../Platform/Metering/Homeport.Platform.Metering.Api/Homeport.Platform.Metering.Api.csproj";
    private const string PortalProject = "../../Platform/Portal/Homeport.Platform.Portal/Homeport.Platform.Portal.csproj";

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
    // replicas: they share its bucket in Valkey (ADR-0007). The compose file
    // runs one, on a fixed host port, so an operator's proxy has an address
    // to point at; scaling out there is the operator's override.
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
        // run as, and metering the admin role it reads every tenant's usage
        // as (ADR-0020); the passwords are generated like the server's own.
        var controlPlaneOwnerPassword = builder.AddParameter("control-plane-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var controlPlaneAppPassword = builder.AddParameter("control-plane-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringOwnerPassword = builder.AddParameter("metering-owner-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringAppPassword = builder.AddParameter("metering-app-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringAdminPassword = builder.AddParameter("metering-admin-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);

        var postgres = builder.AddPostgres("postgres")
            .WithImageTag("18.7-alpine@sha256:b6972e1115a4ff8eba5d7f7d19184926d1e16d7d1136ae1e1a7ebf286da0f015")
            .WithEnvironment("POSTGRES_INITDB_ARGS", InitDbArgs)
            .WithEnvironment("POSTGRES_DB", Database)
            .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", controlPlaneOwnerPassword)
            .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", controlPlaneAppPassword)
            .WithEnvironment("METERING_OWNER_PASSWORD", meteringOwnerPassword)
            .WithEnvironment("METERING_APP_PASSWORD", meteringAppPassword)
            .WithEnvironment("METERING_ADMIN_PASSWORD", meteringAdminPassword)
            .WithInitFiles(InitFiles);
        var postgresEndpoint = postgres.Resource.PrimaryEndpoint;

        // One broker; its init script gives each service a user of its own
        // (ADR-0014), with a password generated like the database's.
        var gatewayRabbitMqPassword = builder.AddParameter("gateway-rabbitmq-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var meteringRabbitMqPassword = builder.AddParameter("metering-rabbitmq-password", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true);
        var rabbitMq = builder.AddRabbitMQ("rabbitmq")
            .WithImageTag("4.4-alpine@sha256:7b77c3d3a996c7fcca1ca8bf53ce1557ff43c13b8ca2c132df6031781b209ef2")
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
        // The realm holds the client the control plane seeds, one it does not
        // know, one the tests register through the admin API, and one that signs
        // the dev operator in by password for the tests; their secrets are
        // generated and never written into the realm file.
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
            .WithImageTag("26.8@sha256:7e66a96e4739239070aeaf72e4c5decf0f7f682cfc725b9f7ea2b336eaba38b3")
            .WithRealmImport(Realms)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_ID", seedClientId)
            .WithEnvironment("HOMEPORT_LOCAL_CLIENT_SECRET", builder.AddParameter("local-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_UNMAPPED_CLIENT_SECRET", builder.AddParameter("unmapped-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_REGISTERED_CLIENT_SECRET", builder.AddParameter("registered-client-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
            .WithEnvironment("HOMEPORT_ADMIN_API_TEST_SECRET", builder.AddParameter("admin-api-test-secret", new GenerateParameterDefault { MinLength = PasswordLength, Special = false }, secret: true, persist: true))
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
        var registry = builder.AddDotnetProject("registry", RegistryProject)
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-registry")
            .WithEnvironment("Registry__RegistrationKeys__echo__0", echoRegistrationKey)
            .WithReference(valkey)
            .WaitFor(valkey)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // Modules. The resource is named after the module id; service.name
        // follows the observability rule instead. Each instance registers at
        // the registry with the address the gateway reaches it at.
        var echo = builder.AddDotnetProject("echo", EchoProject)
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-module-echo")
            .WithEnvironment("Homeport__Registry__Url", $"{Uri.UriSchemeHttp}://registry")
            .WithEnvironment("Homeport__Registry__Key", echoRegistrationKey)
            .WithReference(registry)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);
        echo.WithEnvironment("Homeport__Registry__InstanceAddress", echo.GetEndpoint(Uri.UriSchemeHttp));

        // The gateway's signing key. A local run generates one and keeps it in
        // the user secrets; the compose file takes it from the environment. The
        // additional public key is empty until a rotation needs it, so it is
        // optional: Aspire rejects an empty value for a required parameter.
        var signingKey = builder.AddParameter("gateway-signing-key", new GeneratedSigningKey(), secret: true, persist: true);
        var signingKeyId = builder.AddParameter("gateway-signing-key-id", "local");
        var additionalPublicKey = builder.AddParameter("gateway-additional-public-key", string.Empty).AsOptional();
        var additionalKeyId = builder.AddParameter("gateway-additional-key-id", string.Empty).AsOptional();

        // The gateway's port on the host in the compose file. A local run
        // does not use it; the default only spares the dashboard a prompt.
        var gatewayHostPort = builder.AddParameter("gateway-host-port", new LocalDefault("5100"));

        // The one service with an external endpoint: modules are reachable only
        // through it, on the routes it reads from the registry, for clients with
        // a token from Keycloak that the control plane knows. It applies
        // forwarded headers only from the proxies an operator configures, so
        // it does not take Aspire's publish default of trusting every caller.
        var gateway = builder.AddDotnetProject("gateway", GatewayProject)
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

            // One gateway in the compose file, its external endpoint published
            // on the host port `.env` sets rather than one Docker picks on
            // every start. Single, so a second published port fails the
            // publish instead of sharing the host port.
            .PublishAsDockerComposeService((resource, service) =>
                service.Ports = [$"{gatewayHostPort.AsEnvironmentPlaceholder(resource)}:{service.Ports.Single()}"]);

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
        var controlPlaneMigrate = builder.AddDotnetProject("control-plane-migrate", ControlPlaneProject, WithoutLaunchProfile)
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

        var controlPlane = builder.AddDotnetProject("control-plane", ControlPlaneProject)
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-control-plane")
            .WithEnvironment("ConnectionStrings__control-plane", ConnectionString(postgresEndpoint, "control_plane_app", controlPlaneAppPassword))
            .WithReference(gateway)
            .WaitForCompletion(controlPlaneMigrate)
            .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);

        // The gateway looks clients up at the control plane, and forwards the
        // admin API of tenants and plans to it, which YARP needs the endpoint
        // itself for.
        gateway.WithReference(controlPlane);
        gateway.WithEnvironment("PlatformRoutes__ControlPlaneUrl", controlPlane.GetEndpoint(Uri.UriSchemeHttp));

        // The gateway forwards the admin API of modules to the registry. It
        // reads the route table through service discovery, but YARP takes a
        // destination address as it is.
        gateway.WithEnvironment("PlatformRoutes__RegistryUrl", registry.GetEndpoint(Uri.UriSchemeHttp));

        // Metering: `migrate` applies its migrations as the owner role; the
        // service starts after it completed, runs as the app role and
        // consumes the gateway's usage events with a broker user of its own.
        // It serves the usage export the gateway forwards, with caller tokens
        // it validates against the gateway's JWKS (ADR-0019), and reads the
        // usage of every tenant for the operator as the admin role (ADR-0020).
        var meteringMigrate = builder.AddDotnetProject("metering-migrate", MeteringProject, WithoutLaunchProfile)
            .WithArgs("migrate")
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering-migrate")
            .WithEnvironment("ConnectionStrings__metering-owner", ConnectionString(postgresEndpoint, "metering_owner", meteringOwnerPassword))
            .WaitFor(postgres);

        var metering = builder.AddDotnetProject("metering", MeteringProject)
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-metering")
            .WithEnvironment("ConnectionStrings__metering", ConnectionString(postgresEndpoint, "metering_app", meteringAppPassword))
            .WithEnvironment("ConnectionStrings__metering-admin", ConnectionString(postgresEndpoint, "metering_admin", meteringAdminPassword))
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
        // it, like the gateway, and calls the admin API at the gateway's
        // admin endpoint. A local run serves it over HTTPS with the
        // developer certificate. The compose file publishes plain HTTP: TLS
        // ends at the operator's reverse proxy, whose forwarded headers the
        // portal applies only once the operator trusts it.
        var portal = builder.AddDotnetProject("portal", PortalProject, WithoutLaunchProfile)
            .DisableForwardedHeaders()
            .WithEnvironment("OTEL_SERVICE_NAME", "homeport-portal")
            .WithEnvironment("SignIn__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint(Uri.UriSchemeHttp)}/realms/{Realm}"))
            .WithEnvironment("SignIn__ClientId", PortalClientId)
            .WithEnvironment("SignIn__ClientSecret", portalClientSecret)
            .WithEnvironment("SignIn__RequireHttpsMetadata", bool.FalseString)
            .WithEnvironment("AdminApi__Url", gateway.GetEndpoint(GatewayAdminEndpoint))
            .WaitFor(keycloak);
        if (builder.ExecutionContext.IsRunMode)
        {
            // Development serves the static files from the build output, as
            // a launch profile would; the portal has none here, so the
            // endpoint is the AppHost's alone.
            portal.WithHttpsEndpoint(port: PortalPort, name: PortalEndpoint)
                .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath, endpointName: PortalEndpoint)
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environments.Development)

                // The gateway serves its API docs in a local run only
                // (ADR-0017), so only there the portal links a module to its
                // document, at the address the browser reaches the gateway at.
                .WithEnvironment("ApiDocs__Url", ReferenceExpression.Create($"{gateway.GetEndpoint(Uri.UriSchemeHttp)}{ApiDocsPath}"));
        }
        else
        {
            portal.WithHttpEndpoint()
                .WithHttpHealthCheck(ServiceDefaultsExtensions.ReadinessPath);
        }

        portal.WithExternalHttpEndpoints();

        builder.Build().Run();
    }

    /// <summary>
    /// A role's connection to the Homeport database, built from the server's
    /// endpoint, so it resolves locally and in the compose file alike.
    /// </summary>
    private static ReferenceExpression ConnectionString(EndpointReference postgres, string role, IResourceBuilder<ParameterResource> password)
    {
        return ReferenceExpression.Create(
            $"Host={postgres.Property(EndpointProperty.Host)};Port={postgres.Property(EndpointProperty.Port)};Database={Database};Username={role};Password={password}");
    }

    /// <summary>
    /// A service's connection to the broker as its own user, built from the
    /// broker's endpoint like the database connections.
    /// </summary>
    private static ReferenceExpression BrokerConnectionString(EndpointReference rabbitMq, string user, IResourceBuilder<ParameterResource> password)
    {
        return ReferenceExpression.Create(
            $"amqp://{user}:{password}@{rabbitMq.Property(EndpointProperty.Host)}:{rabbitMq.Property(EndpointProperty.Port)}");
    }

    /// <summary>
    /// Runs a project without its launch profile, for a one-shot command
    /// and for a resource whose endpoints the AppHost declares itself.
    /// </summary>
    private static void WithoutLaunchProfile(ProjectResourceOptions options)
    {
        options.ExcludeLaunchProfile = true;
    }
}
