using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Micro.Shared.Http.Extensions;
using Micro.Shared.Middleware;
using Micro.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Shared components
builder.Services.AddOutboundHttpInfrastructure(builder.Environment.ApplicationName);
builder.Services.AddPaymentServiceClient(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom Middlewares for Multi-tenancy and DB Routing
app.UseMiddleware<CountryMiddleware>();
app.UseMiddleware<OperationModeMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Database Migration on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    // Define all tenants and their modes to ensure every database is migrated
    var countries = new[] { "Egypt", "UAE" };
    var modes = new[] { OperationMode.Write, OperationMode.Read };

    foreach (var country in countries)
    {
        foreach (var mode in modes)
        {
            try
            {
                // We must create a new scope for each migration because the connection string 
                // is resolved from the RequestContext at the moment the DbContext is instantiated.
                using (var migrationScope = app.Services.CreateScope())
                {
                    var requestContext = migrationScope.ServiceProvider.GetRequiredService<IRequestContext>();
                    requestContext.Country = country;
                    requestContext.OperationMode = mode;

                    var context = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    logger.LogInformation("Applying migrations for {Country} ({Mode})...", country, mode);
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Successfully migrated {Country} ({Mode}).", country, mode);
                }
            }
            catch (Exception ex)
            {
                // We use Warning because some databases (like UAE) might not be configured/reachable yet in all environments
                logger.LogWarning(ex, "Migration skipped for {Country} ({Mode}): {Message}", country, mode, ex.Message);
            }
        }
    }
}

await app.RunAsync();