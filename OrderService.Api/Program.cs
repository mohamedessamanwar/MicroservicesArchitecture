using OrderService.Application;
using OrderService.Infrastructure;
using Micro.Shared.Caching;
using Micro.Shared.Health;
using Micro.Shared.Http.Extensions;
using Micro.Shared.Middleware;
using Micro.Shared.RateLimiting.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// StackExchange.Redis: single ConnectionMultiplexer (Singleton) + scoped IRedisRepository using shared IDatabase.
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddOutboundHttpInfrastructure();
builder.Services.AddPaymentServiceClient(builder.Configuration);
builder.Services.AddProductServiceClient(builder.Configuration);
builder.Services.AddMicroserviceHealthChecks(builder.Configuration);

// Add Protection Services
builder.Services.AddRequestTimeouts(options =>
{
    var timeoutStr = builder.Configuration["RequestTimeouts:DefaultTimeout"] ?? "00:00:10";
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.Parse(timeoutStr)
    };
});
builder.Services.AddDistributedRateLimiter(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseForwardedHeaders(); // Trust the gateway IP
app.UseRequestTimeouts();  // Start execution timer
app.UseHttpsRedirection();

app.UseRouting();
app.MapMicroserviceHealthChecks();

// Custom Middlewares for Multi-tenancy and DB Routing
app.UseMiddleware<CountryMiddleware>();
app.UseMiddleware<OperationModeMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseDistributedRateLimiter(); // Applies User/IP limits

app.MapControllers();

await app.RunAsync();