namespace CommerceFlow.Contracts.Messaging;

public static class MessagingTopology
{
    public const string EventsExchange = "commerceflow.events";
    public const string DeadLetterExchange = "commerceflow.dead-letter";

    public const string InventoryQueue = "inventory.order-events.v1";
    public const string InventoryDeadLetterQueue = "inventory.order-events.v1.dlq";
    public const string InventoryDeadLetterRoutingKey = "inventory.order-events.dead";

    public const string SalesQueue = "sales.stock-results.v1";
    public const string SalesDeadLetterQueue = "sales.stock-results.v1.dlq";
    public const string SalesDeadLetterRoutingKey = "sales.stock-results.dead";

    public const string OrderCreatedRoutingKey = "sales.order.created.v1";
    public const string StockReleaseRequestedRoutingKey = "sales.stock.release-requested.v1";
    public const string StockReservedRoutingKey = "inventory.stock.reserved.v1";
    public const string StockRejectedRoutingKey = "inventory.stock.rejected.v1";
}
