// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Shared.Client.Problems;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Homeport.Sdk.Registration;

/// <summary>
/// Registers the instance at the registry once it has started and keeps it
/// registered with a heartbeat every <see cref="HeartbeatInterval"/>. A
/// heartbeat the registry answers with <c>404</c> registers the instance again.
/// </summary>
/// <remarks>
/// A registration the registry refuses - a wrong key, a manifest that breaks
/// the module contract - stops the module: it would run without ever being
/// routed to. A registry that cannot be reached is retried at every interval.
/// </remarks>
internal sealed class ModuleRegistration(
    IHttpClientFactory clients,
    IOptions<RegistrationOptions> options,
    ModuleManifest manifest,
    IHostApplicationLifetime lifetime,
    TimeProvider time,
    ILogger<ModuleRegistration> logger)
    : BackgroundService
{
    /// <summary>The name of the HTTP client that calls the registry.</summary>
    internal const string ClientName = "Homeport.Registry";

    /// <summary>How often the instance renews its registration; the registry drops it after 30 s.</summary>
    internal static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    // The registration key travels as a bearer token (ADR-0012).
    private const string BearerScheme = "Bearer";

    // Unique per running instance, so instances of one module never replace each other.
    private readonly string instanceId = Guid.NewGuid().ToString("N");

    private bool registered;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (settings.Url is null)
        {
            logger.LogWarning(
                "Module {ModuleId} does not register: {Section}:Url is not set, so the gateway does not route to it",
                manifest.Id,
                RegistrationOptions.SectionName);
            return;
        }

        if (!await StartedAsync(stoppingToken))
        {
            return;
        }

        using var timer = new PeriodicTimer(HeartbeatInterval, time);
        do
        {
            if (!await TickAsync(settings, stoppingToken))
            {
                lifetime.StopApplication();
                return;
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    // Registers only once the module serves requests, so the gateway never
    // forwards to an instance that is not listening yet.
    private async Task<bool> StartedAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource();
        using var onStarted = lifetime.ApplicationStarted.Register(started.SetResult);
        using var onStopping = stoppingToken.Register(() => started.TrySetCanceled(stoppingToken));

        try
        {
            await started.Task;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    // False when the registry refused the instance for good.
    private async Task<bool> TickAsync(RegistrationOptions settings, CancellationToken stoppingToken)
    {
        try
        {
            if (registered && await HeartbeatAsync(settings, stoppingToken))
            {
                return true;
            }

            return await RegisterAsync(settings, stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // Unreachable, timed out or answering with an error: the next tick tries again.
            logger.LogWarning(
                exception,
                "Module {ModuleId} could not reach the registry at {Url}; retrying in {Interval}",
                manifest.Id,
                settings.Url,
                HeartbeatInterval);
            return true;
        }
    }

    // False when the registry no longer knows the instance.
    private async Task<bool> HeartbeatAsync(RegistrationOptions settings, CancellationToken stoppingToken)
    {
        using var response = await Client(settings).PostAsync(Path(RegistryPaths.Heartbeat), content: null, stoppingToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("The registry no longer knows instance {InstanceId} of module {ModuleId}; registering again", instanceId, manifest.Id);
            registered = false;
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    // False when the registry refused the instance for good.
    private async Task<bool> RegisterAsync(RegistrationOptions settings, CancellationToken stoppingToken)
    {
        var request = new RegisterInstanceRequest(settings.InstanceAddress!, manifest);
        using var response = await Client(settings).PutAsJsonAsync(Path(RegistryPaths.Instance), request, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            registered = true;
            logger.LogInformation(
                "Instance {InstanceId} of module {ModuleId} registered at {Url} as {InstanceAddress}",
                instanceId,
                manifest.Id,
                settings.Url,
                settings.InstanceAddress);
            return true;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.UnprocessableEntity)
        {
            await LogRefusalAsync(response, stoppingToken);

            // A module the registry refuses is a broken deployment, not a clean stop.
            Environment.ExitCode = 1;
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task LogRefusalAsync(HttpResponseMessage response, CancellationToken stoppingToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(stoppingToken);
        var errors = problem?.Errors?.SelectMany(field => field.Value.Select(message => $"{field.Key}: {message}")) ?? [];

        logger.LogError(
            "The registry refused module {ModuleId} with {StatusCode} {Title}; the module stops. {Errors}",
            manifest.Id,
            (int)response.StatusCode,
            problem?.Title,
            errors);
    }

    private HttpClient Client(RegistrationOptions settings)
    {
        var client = clients.CreateClient(ClientName);
        client.BaseAddress = settings.Url;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, settings.Key);
        return client;
    }

    private string Path(string template)
    {
        return template
            .Replace("{moduleId}", manifest.Id, StringComparison.Ordinal)
            .Replace("{instanceId}", instanceId, StringComparison.Ordinal);
    }
}
