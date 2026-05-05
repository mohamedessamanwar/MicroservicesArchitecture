using FluentValidation.AspNetCore;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Micro.Shared.Http.Extensions;

using Micro.Shared.Middleware;
using Micro.Shared.Caching;
using Micro.Shared.Persistence;
using Micro.Shared.Idempotency;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();

// Layer registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddIdempotency();

// Microservice Clients
builder.Services.AddOutboundHttpInfrastructure();
builder.Services.AddOrderServiceClient(builder.Configuration);


var app = builder.Build();

// Database Migration on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    var countries = new[] { "Egypt", "UAE" };
    var modes = new[] { OperationMode.Write, OperationMode.Read };

    foreach (var country in countries)
    {
        foreach (var mode in modes)
        {
            try
            {
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
                logger.LogWarning(ex, "Migration skipped for {Country} ({Mode}): {Message}", country, mode, ex.Message);
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CountryMiddleware>();
app.UseMiddleware<OperationModeMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();