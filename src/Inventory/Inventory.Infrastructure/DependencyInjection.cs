using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:InventoryDatabase é obrigatória.");

        services.AddDbContextFactory<InventoryDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
                sqlServer.EnableRetryOnFailure(maxRetryCount: 8, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<InventoryService>();
        services.AddSingleton(RabbitMqSettings.From(configuration));
        services.AddHostedService<InventoryOutboxPublisher>();
        services.AddHostedService<OrderEventsConsumer>();

        services.AddHealthChecks()
            .AddCheck<InventoryDatabaseHealthCheck>("inventory-database", tags: ["ready"])
            .AddCheck<RabbitMqHealthCheck>("inventory-rabbitmq", tags: ["ready"]);

        return services;
    }
}
