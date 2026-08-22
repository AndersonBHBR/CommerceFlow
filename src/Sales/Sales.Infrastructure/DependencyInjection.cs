using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Abstractions;
using Sales.Application.Orders;
using Sales.Infrastructure.Inventory;
using Sales.Infrastructure.Messaging;
using Sales.Infrastructure.Persistence;

namespace Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SalesDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:SalesDatabase é obrigatória.");
        var inventoryBaseUrl = configuration["Services:InventoryBaseUrl"]
            ?? throw new InvalidOperationException("Services:InventoryBaseUrl é obrigatória.");
        if (!Uri.TryCreate(inventoryBaseUrl, UriKind.Absolute, out var inventoryUri))
        {
            throw new InvalidOperationException("Services:InventoryBaseUrl deve ser uma URL absoluta.");
        }

        services.AddDbContextFactory<SalesDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
                sqlServer.EnableRetryOnFailure(maxRetryCount: 8, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

        services
            .AddHttpClient<IInventoryCatalog, InventoryCatalogClient>(client =>
            {
                client.BaseAddress = inventoryUri;
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.Retry.MaxRetryAttempts = 2;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IIntegrationOutbox, IntegrationOutbox>();
        services.AddScoped<OrderService>();
        services.AddSingleton(RabbitMqSettings.From(configuration));
        services.AddHostedService<SalesOutboxPublisher>();
        services.AddHostedService<StockResultConsumer>();

        services.AddHealthChecks()
            .AddCheck<SalesDatabaseHealthCheck>("sales-database", tags: ["ready"])
            .AddCheck<RabbitMqHealthCheck>("sales-rabbitmq", tags: ["ready"]);

        return services;
    }
}
