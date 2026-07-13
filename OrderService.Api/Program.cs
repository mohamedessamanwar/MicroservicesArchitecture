using OrderService.Application;
using OrderService.Infrastructure;
using Micro.Shared.Caching;
using Micro.Shared.Health;
using Micro.Shared.Http.Extensions;
using Micro.Shared.Middleware;

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
builder.Services.AddMicroserviceHealthChecks(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();
app.MapMicroserviceHealthChecks();

// Custom Middlewares for Multi-tenancy and DB Routing
app.UseMiddleware<CountryMiddleware>();
app.UseMiddleware<OperationModeMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();