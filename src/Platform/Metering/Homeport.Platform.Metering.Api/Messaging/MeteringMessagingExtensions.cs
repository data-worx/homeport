// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Homeport.Platform.Metering.Api.Messaging;

/// <summary>Wires metering to RabbitMQ through Wolverine (ADR-0007).</summary>
internal static class MeteringMessagingExtensions
{
    /// <summary>The connection string of metering's own broker user (ADR-0014).</summary>
    internal const string ConnectionName = "rabbitmq";

    // A race at a unique key ends in a DbUpdateException; the next attempt
    // finds the row the winner wrote. More attempts than that point at a
    // real failure, and the message goes to the dead-letter queue.
    private const int RetriesOnConflict = 3;

    /// <summary>
    /// Consumes <see cref="UsageMessaging.MeteringQueue"/>, bound to
    /// <see cref="UsageMessaging.Exchange"/>, and acknowledges a message only
    /// once it is handled, so a crash redelivers it rather than losing it.
    /// </summary>
    internal static IHostApplicationBuilder AddMeteringMessaging(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException($"The connection string '{ConnectionName}' is missing.");

        builder.Services.AddSingleton<MeteringMetrics>();
        builder.UseWolverine(options =>
        {
            options.ApplicationAssembly = typeof(MeteringMessagingExtensions).Assembly;

            // The store's database context is registered through a factory,
            // which Wolverine's generated code cannot construct inline; the
            // message's scope resolves the store instead.
            options.CodeGeneration.AlwaysUseServiceLocationFor<IUsageStore>();

            // No reply queue: metering answers no requests, and its broker
            // user may declare only its own queues.
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .DisableSystemRequestReplyQueueDeclaration()
                .DeclareExchange(UsageMessaging.Exchange, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Fanout;
                    exchange.BindQueue(UsageMessaging.MeteringQueue);
                });

            options.ListenToRabbitQueue(UsageMessaging.MeteringQueue).ProcessInline();
            options.OnException<DbUpdateException>().RetryTimes(RetriesOnConflict);
        });

        return builder;
    }
}
