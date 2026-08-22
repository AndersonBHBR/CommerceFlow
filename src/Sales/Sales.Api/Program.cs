using System.Text.Json.Serialization;
using CommerceFlow.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Sales.Api.Endpoints;
using Sales.Infrastructure;
using Sales.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddCommerceFlowJwtAuthentication();

builder.Services.AddOpenApi("v1");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSalesInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var factory = app.Services.GetRequiredService<IDbContextFactory<SalesDbContext>>();
    await using var dbContext = await factory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapOrderEndpoints();

app.Run();

public partial class Program
{
}
