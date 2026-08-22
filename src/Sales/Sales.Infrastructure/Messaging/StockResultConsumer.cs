using System.Net.Sockets;
using System.Text.Json;
using CommerceFlow.Contracts.Events;
using CommerceFlow.Contracts.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Sales.Domain.Orders;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Persistence.Messaging;

namespace Sales.Infrastructure.Messaging;

internal sealed class StockResultConsumer(
    IDbContextFactory<SalesDbContext> dbContextFactory,
    RabbitMqSettings settings,
    TimeProvider timeProvider,
    ILogger<StockResultConsumer> logger) : BackgroundService
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
                MessagingLog.RabbitMqUnavailable(logger, nameof(StockResultConsumer), exception);
            }
            catch (AlreadyClosedException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(StockResultConsumer), exception);
            }
            catch (OperationInterruptedException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(StockResultConsumer), exception);
            }
            catch (IOException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(StockResultConsumer), exception);
            }
            catch (SocketException exception)
            {
                MessagingLog.RabbitMqUnavailable(logger, nameof(StockResultConsumer), exception);
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
            MessagingTopology.SalesQueue,
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
                case MessagingTopology.StockReservedRoutingKey:
                    var reserved = JsonSerializer.Deserialize<StockReservedV1>(body, SerializerOptions)
                        ?? throw new JsonException("Evento StockReservedV1 vazio.");
                    Validate(reserved);
                    await ApplyReservedAsync(reserved, cancellationToken);
                    break;

                case MessagingTopology.StockRejectedRoutingKey:
                    var rejected = JsonSerializer.Deserialize<StockRejectedV1>(body, SerializerOptions)
                        ?? throw new JsonException("Evento StockRejectedV1 vazio.");
                    Validate(rejected);
                    await ApplyRejectedAsync(rejected, cancellationToken);
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

    private async Task ApplyReservedAsync(StockReservedV1 message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(item => item.MessageId == message.MessageId, cancellationToken))
        {
            return;
        }

        var order = await dbContext.Orders.SingleOrDefaultAsync(
            item => item.Id == message.OrderId,
            cancellationToken)
            ?? throw new InvalidOperationException($"Pedido {message.OrderId} não encontrado para confirmação.");

        if (order.Status == OrderStatus.PendingStock)
        {
            order.ConfirmStock();
        }
        else if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Cancelled))
        {
            throw new InvalidOperationException($"Pedido {message.OrderId} não aceita confirmação no estado {order.Status}.");
        }

        dbContext.InboxMessages.Add(InboxMessage.Create(
            message.MessageId,
            typeof(StockReservedV1).FullName ?? nameof(StockReservedV1),
            timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyRejectedAsync(StockRejectedV1 message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.InboxMessages.AnyAsync(item => item.MessageId == message.MessageId, cancellationToken))
        {
            return;
        }

        var order = await dbContext.Orders.SingleOrDefaultAsync(
            item => item.Id == message.OrderId,
            cancellationToken)
            ?? throw new InvalidOperationException($"Pedido {message.OrderId} não encontrado para rejeição.");

        if (order.Status == OrderStatus.PendingStock)
        {
            var detail = message.Items.Count == 0
                ? message.ReasonCode.ToString()
                : string.Join("; ", message.Items.Select(item => item.Detail));
            order.RejectStock(detail.Length <= 500 ? detail : detail[..500]);
        }
        else if (order.Status is not (OrderStatus.Rejected or OrderStatus.Cancelled))
        {
            throw new InvalidOperationException($"Pedido {message.OrderId} não aceita rejeição no estado {order.Status}.");
        }

        dbContext.InboxMessages.Add(InboxMessage.Create(
            message.MessageId,
            typeof(StockRejectedV1).FullName ?? nameof(StockRejectedV1),
            timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

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

    private static void Validate(StockReservedV1 message)
    {
        if (message.MessageId == Guid.Empty || message.OrderId == Guid.Empty)
        {
            throw new JsonException("StockReservedV1 deve informar MessageId e OrderId.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId) || message.CorrelationId.Length > 128)
        {
            throw new JsonException("StockReservedV1 possui CorrelationId inválido.");
        }

        if (message.Items is null || message.Items.Count < 1
            || message.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity < 1))
        {
            throw new JsonException("StockReservedV1 possui itens inválidos.");
        }
    }

    private static void Validate(StockRejectedV1 message)
    {
        if (message.MessageId == Guid.Empty || message.OrderId == Guid.Empty)
        {
            throw new JsonException("StockRejectedV1 deve informar MessageId e OrderId.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId) || message.CorrelationId.Length > 128)
        {
            throw new JsonException("StockRejectedV1 possui CorrelationId inválido.");
        }

        if (!Enum.IsDefined(message.ReasonCode)
            || message.Items is null
            || message.Items.Count < 1
            || message.Items.Any(item =>
                item.ProductId == Guid.Empty
                || item.Quantity < 1
                || string.IsNullOrWhiteSpace(item.Detail)))
        {
            throw new JsonException("StockRejectedV1 possui motivo ou itens inválidos.");
        }
    }
}
