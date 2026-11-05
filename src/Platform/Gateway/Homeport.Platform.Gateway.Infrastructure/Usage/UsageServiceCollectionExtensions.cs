// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Homeport.Platform.Gateway.Infrastructure.Usage;

/// <summary>Wires the gateway's publishing of usage events through Wolverine to RabbitMQ (ADR-0007).</summary>
public static class UsageServiceCollectionExtensions
{
    /// <summary>The connection string of the gateway's own broker user (ADR-0014).</summary>
    public const string ConnectionName = "rabbitmq";

    /// <summary>
    /// Adds <see cref="UsagePublisher"/>, which sends to <see cref="UsageMessaging.Exchange"/>
    /// without an outbox: buffered in memory, with publisher confirms, retried
    /// after <see cref="UsageOptions.EffectiveRetryDelays"/>, then dropped and counted.
    /// </summary>
    public static IHostApplicationBuilder AddUsagePublishing(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException($"The connection string '{ConnectionName}' is missing.");
        var options = builder.Configuration.GetSection(UsageOptions.SectionName).Get<UsageOptions>() ?? new UsageOptions();

        builder.Services.AddOptions<UsageOptions>().BindConfiguration(UsageOptions.SectionName);
        builder.Services.AddSingleton<UsageMetrics>();
        builder.Services.AddScoped<UsagePublisher>();

        builder.UseWolverine(wolverine =>
        {
            wolverine.ApplicationAssembly = typeof(UsageServiceCollectionExtensions).Assembly;

            // The gateway only publishes: no listener, no reply queue and no
            // dead-letter queue of its own, so its broker user needs nothing
            // but writing to the exchange.
            wolverine.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .UseSenderConnectionOnly()
                .DisableSystemRequestReplyQueueDeclaration()
                .DisableDeadLetterQueueing()
                .ConfigureChannelCreation(channel =>
                {
                    channel.PublisherConfirmationsEnabled = true;
                    channel.PublisherConfirmationTrackingEnabled = true;
                })
                .DeclareExchange(UsageMessaging.Exchange, exchange => exchange.ExchangeType = ExchangeType.Fanout);

            wolverine.PublishMessage<UsageRecorded>()
                .ToRabbitExchange(UsageMessaging.Exchange)
                .BufferedInMemory()
                .ConfigureSending(sending => sending
                    .OnException<Exception>()
                    .RetryWithCooldown([.. options.EffectiveRetryDelays])
                    .Then.CustomAction(DropAsync, "Count the usage event as dropped"));
        });

        return builder;
    }

    // After the last retry: counted, logged, and completed, so Wolverine
    // neither sends it again nor trips the circuit breaker over it.
    private static async ValueTask DropAsync(Wolverine.Runtime.IWolverineRuntime runtime, IEnvelopeLifecycle lifecycle, Exception exception)
    {
        runtime.Services.GetRequiredService<UsageMetrics>().Dropped(UsageMetrics.RetriesExhausted);
        runtime.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(UsagePublisher))
            .LogWarning(exception, "Usage event {EventId} was not taken by the broker after every retry and is dropped", (lifecycle.Envelope?.Message as UsageRecorded)?.EventId);
        await lifecycle.CompleteAsync();
    }
}
