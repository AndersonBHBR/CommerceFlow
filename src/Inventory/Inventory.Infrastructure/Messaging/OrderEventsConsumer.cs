using System.Net.Sockets;
using System.Text.Json;
using CommerceFlow.Contracts.Events;
using CommerceFlow.Contracts.Messaging;
using Inventory.Domain.Products;
using Inventory.Domain.Stock;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Inventory.Infrastructure.Messaging;

internal sealed class OrderEventsConsumer(
    IDbContextFactory<InventoryDbContext> dbContextFactory,
    RabbitMqSettings settings,
    TimeProvider timeProvider,
    ILogger<OrderEventsConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (BrokerUnreachableException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(OrderEventsConsumer), exception);
            }
            catch (AlreadyClosedException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(OrderEventsConsumer), exception);
            }
            catch (OperationInterruptedException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(OrderEventsConsumer), exception);
            }
            catch (IOException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(OrderEventsConsumer), exception);
            }
            catch (SocketException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(OrderEventsConsumer), exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        var factory = settings.CreateConnectionFactory();
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMqTopologyInitializer.DeclareAsync(channel, cancellationToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, cancellationToken);
        await channel.BasicConsumeAsync(
            MessagingTopology.InventoryQueue,
            autoAck: false,
            consumer,
            cancellationToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        var body = delivery.Body.ToArray();
        using var activity = MessagingTelemetry.StartConsume(delivery.RoutingKey, delivery.BasicProperties.CorrelationId);
        try
        {
            switch (delivery.RoutingKey)
            {
                case MessagingTopology.OrderCreatedRoutingKey:
                    var created = JsonSerializer.Deserialize<OrderCreatedV1>(body, SerializerOptions)
                        ?? throw new JsonException("Evento OrderCreatedV1 vazio.");
                    Validate(created);
                    await ReserveAsync(created, cancellationToken);
                    break;

                case MessagingTopology.StockReleaseRequestedRoutingKey:
                    var release = JsonSerializer.Deserialize<StockReleaseRequestedV1>(body, SerializerOptions)
                        ?? throw new JsonException("Evento StockReleaseRequestedV1 vazio.");
                    Validate(release);
                    await ReleaseAsync(release, cancellationToken);
                    break;

                default:
                    throw new JsonException($"Routing key desconhecida: {delivery.RoutingKey}.");
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
            MessagingTelemetry.RecordProcessed(delivery.RoutingKey);
        }
        catch (JsonException exception)
        {
            MessagingTelemetry.RecordFailed(delivery.RoutingKey);
            MessagingLog.PoisonMessage(logger, delivery.RoutingKey, exception);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            MessagingTelemetry.RecordFailed(delivery.RoutingKey);
            await HandleTransientFailureAsync(channel, delivery, exception, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            MessagingTelemetry.RecordFailed(delivery.RoutingKey);
            await HandleTransientFailureAsync(channel, delivery, exception, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            MessagingTelemetry.RecordFailed(delivery.RoutingKey);
            await HandleTransientFailureAsync(channel, delivery, exception, cancellationToken);
        }
    }

    private async Task ReserveAsync(OrderCreatedV1 message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(item => item.MessageId == message.MessageId, cancellationToken))
        {
            return;
        }

        if (await dbContext.StockReservations.AnyAsync(
                item => item.OrderId == message.OrderId,
                cancellationToken))
        {
            dbContext.InboxMessages.Add(CreateInbox(message));
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var productIds = message.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToArrayAsync(cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        var rejection = Validate(message, productsById);
        var now = timeProvider.GetUtcNow();

        if (rejection is not null)
        {
            AddOutbox(dbContext, new StockRejectedV1(
                Guid.CreateVersion7(),
                now,
                message.CorrelationId,
                message.OrderId,
                rejection.Value.Reason,
                rejection.Value.Items));
        }
        else
        {
            foreach (var item in message.Items)
            {
                var product = productsById[item.ProductId];
                product.ReserveStock(item.Quantity);
                dbContext.StockReservations.Add(StockReservation.Create(
                    message.OrderId,
                    item.ProductId,
                    item.Quantity,
                    now));
            }

            AddOutbox(dbContext, new StockReservedV1(
                Guid.CreateVersion7(),
                now,
                message.CorrelationId,
                message.OrderId,
                message.Items
                    .Select(item => new ReservedStockItemV1(item.ProductId, item.Quantity))
                    .ToArray()));
        }

        dbContext.InboxMessages.Add(CreateInbox(message));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReleaseAsync(StockReleaseRequestedV1 message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(item => item.MessageId == message.MessageId, cancellationToken))
        {
            return;
        }

        var reservations = await dbContext.StockReservations
            .Where(item => item.OrderId == message.OrderId && item.ReleasedAtUtc == null)
            .ToArrayAsync(cancellationToken);
        if (reservations.Length > 0)
        {
            var productIds = reservations.Select(item => item.ProductId).ToArray();
            var products = await dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, cancellationToken);
            var now = timeProvider.GetUtcNow();

            foreach (var reservation in reservations)
            {
                if (!products.TryGetValue(reservation.ProductId, out var product))
                {
                    throw new InvalidOperationException($"Produto {reservation.ProductId} da reserva não foi encontrado.");
                }

                product.ReleaseStock(reservation.Quantity);
                reservation.Release(now);
            }
        }

        dbContext.InboxMessages.Add(CreateInbox(message));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (StockRejectionReasonV1 Reason, IReadOnlyCollection<RejectedStockItemV1> Items)? Validate(
        OrderCreatedV1 message,
        Dictionary<Guid, Product> products)
    {
        var rejectedItems = new List<RejectedStockItemV1>();
        var reason = StockRejectionReasonV1.InsufficientStock;

        foreach (var item in message.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                reason = StockRejectionReasonV1.ProductNotFound;
                rejectedItems.Add(new RejectedStockItemV1(
                    item.ProductId,
                    item.Quantity,
                    "Produto não encontrado."));
            }
            else if (!product.IsActive)
            {
                if (reason != StockRejectionReasonV1.ProductNotFound)
                {
                    reason = StockRejectionReasonV1.ProductInactive;
                }

                rejectedItems.Add(new RejectedStockItemV1(
                    item.ProductId,
                    item.Quantity,
                    $"Produto {product.Sku} está inativo."));
            }
            else if (product.FreeQuantity < item.Quantity)
            {
                rejectedItems.Add(new RejectedStockItemV1(
                    item.ProductId,
                    item.Quantity,
                    $"Saldo livre insuficiente para {product.Sku}. Disponível: {product.FreeQuantity}."));
            }
        }

        return rejectedItems.Count == 0 ? null : (reason, rejectedItems);
    }

    private static void Validate(OrderCreatedV1 message)
    {
        if (message.MessageId == Guid.Empty || message.OrderId == Guid.Empty)
        {
            throw new JsonException("OrderCreatedV1 deve informar MessageId e OrderId.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId) || message.CorrelationId.Length > 128)
        {
            throw new JsonException("OrderCreatedV1 possui CorrelationId inválido.");
        }

        if (message.Items is null || message.Items.Count is < 1 or > 100)
        {
            throw new JsonException("OrderCreatedV1 deve possuir entre 1 e 100 itens.");
        }

        if (message.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity < 1)
            || message.Items.Select(item => item.ProductId).Distinct().Count() != message.Items.Count)
        {
            throw new JsonException("OrderCreatedV1 possui item inválido ou duplicado.");
        }
    }

    private static void Validate(StockReleaseRequestedV1 message)
    {
        if (message.MessageId == Guid.Empty || message.OrderId == Guid.Empty)
        {
            throw new JsonException("StockReleaseRequestedV1 deve informar MessageId e OrderId.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId) || message.CorrelationId.Length > 128)
        {
            throw new JsonException("StockReleaseRequestedV1 possui CorrelationId inválido.");
        }
    }

    private static void AddOutbox<T>(InventoryDbContext dbContext, T message)
        where T : class, IIntegrationEvent
    {
        var type = typeof(T).FullName ?? typeof(T).Name;
        var routingKey = message switch
        {
            StockReservedV1 => MessagingTopology.StockReservedRoutingKey,
            StockRejectedV1 => MessagingTopology.StockRejectedRoutingKey,
            _ => throw new InvalidOperationException($"Evento de saída não suportado: {type}.")
        };
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            message.MessageId,
            message.OccurredAtUtc,
            message.CorrelationId,
            type,
            routingKey,
            JsonSerializer.Serialize(message, SerializerOptions)));
    }

    private InboxMessage CreateInbox(IIntegrationEvent message) => InboxMessage.Create(
        message.MessageId,
        message.GetType().FullName ?? message.GetType().Name,
        timeProvider.GetUtcNow());

    private async Task HandleTransientFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var requeue = !delivery.Redelivered;
        MessagingLog.ProcessingFailed(logger, delivery.RoutingKey, requeue, exception);
        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue, cancellationToken);
    }
}
