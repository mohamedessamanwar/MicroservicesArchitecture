using FluentValidation.AspNetCore;
using Micro.Shared.Caching;
using Micro.Shared.Http.Extensions;
using Micro.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();

if (EF.IsDesignTime)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDatabase")));
}

// Layer registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddIdempotency();

// Microservice Clients
builder.Services.AddOutboundHttpInfrastructure();
builder.Services.AddOrderServiceClient(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseMiddleware<CountryMiddleware>();
app.UseMiddleware<OperationModeMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
