using ProductService.Infrastructure.Data;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Micro.Shared.Health;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health Checks for Docker
builder.Services.AddMicroserviceHealthChecks(builder.Configuration);
// Register MediatR for Application layer
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("ProductService.Application")));

// Register DB Context and Repository
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapMicroserviceHealthChecks();
// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
