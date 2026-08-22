using CommerceFlow.Contracts.Messaging;
using RabbitMQ.Client;

namespace Inventory.Infrastructure.Messaging;

internal static class RabbitMqTopologyInitializer
{
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            MessagingTopology.EventsExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            MessagingTopology.DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await DeclareQueueAsync(
            channel,
            MessagingTopology.InventoryQueue,
            MessagingTopology.InventoryDeadLetterQueue,
            MessagingTopology.InventoryDeadLetterRoutingKey,
            [MessagingTopology.OrderCreatedRoutingKey, MessagingTopology.StockReleaseRequestedRoutingKey],
            cancellationToken);
        await DeclareQueueAsync(
            channel,
            MessagingTopology.SalesQueue,
            MessagingTopology.SalesDeadLetterQueue,
            MessagingTopology.SalesDeadLetterRoutingKey,
            [MessagingTopology.StockReservedRoutingKey, MessagingTopology.StockRejectedRoutingKey],
            cancellationToken);
    }

    private static async Task DeclareQueueAsync(
        IChannel channel,
        string queue,
        string deadLetterQueue,
        string deadLetterRoutingKey,
        IReadOnlyCollection<string> routingKeys,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            deadLetterQueue,
            MessagingTopology.DeadLetterExchange,
            deadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = MessagingTopology.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = deadLetterRoutingKey
        };
        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(
                queue,
                MessagingTopology.EventsExchange,
                routingKey,
                cancellationToken: cancellationToken);
        }
    }
}
