using CommerceFlow.ServiceDefaults;
using Inventory.Api.Endpoints;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddCommerceFlowJwtAuthentication();

builder.Services.AddOpenApi("v1");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInventoryInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var factory = app.Services.GetRequiredService<IDbContextFactory<InventoryDbContext>>();
    await using var dbContext = await factory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapInventoryEndpoints();

app.Run();

public partial class Program
{
}
