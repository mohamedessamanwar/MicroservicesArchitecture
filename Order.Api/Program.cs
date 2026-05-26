using Micro.Shared.MetricServices.Extensions;
using Order.Api.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNativeMetricMonitoring(builder.Configuration);
builder.Services.AddScoped<IMetricMonitoringQueryHandler, MetricMonitoringQueryHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseNativeMetricMonitoring();

app.UseAuthorization();

app.MapControllers();

app.Run();
