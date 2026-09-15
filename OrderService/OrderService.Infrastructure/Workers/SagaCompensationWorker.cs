using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Clients.Product;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Workers;

public class SagaCompensationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaCompensationWorker> _logger;

    public SagaCompensationWorker(IServiceProvider serviceProvider, ILogger<SagaCompensationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SagaCompensationWorker started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCompensationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SagaCompensationWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessCompensationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var productServiceClient = scope.ServiceProvider.GetRequiredService<IProductServiceClient>();

        var compensatingSagas = await dbContext.Sagas
            .Include(s => s.Steps)
            .Where(s => s.Status == SagaStatus.Compensating)
            .ToListAsync(cancellationToken);

        foreach (var saga in compensatingSagas)
        {
            bool allCompensated = true;

            var reserveStep = saga.Steps.FirstOrDefault(s => s.StepName == OrderService.Application.Constants.SagaConstants.Steps.ReserveInventory);
            if (reserveStep != null && reserveStep.CompensationStatus != CompensationStatus.Completed && reserveStep.CompensationStatus != CompensationStatus.None)
            {
                allCompensated = false;
                
                if (reserveStep.Payload != null)
                {
                    try
                    {
                        var itemsToCompensate = System.Text.Json.JsonSerializer.Deserialize<List<ProductQuantityDto>>(
                            reserveStep.Payload,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (itemsToCompensate == null || !itemsToCompensate.Any())
                        {
                            throw new Exception("Payload is empty or invalid for reserve compensation");
                        }

                        reserveStep.CompensationStatus = CompensationStatus.Executing;
                        await dbContext.SaveChangesAsync(cancellationToken);

                        var result = await productServiceClient.IncreaseStockBulkAsync(
                            itemsToCompensate, 
                            idempotencyKey: $"{saga.CorrelationId}-reserve-comp",
                            cancellationToken: cancellationToken);

                        if (result.Success)
                        {
                            reserveStep.CompensationStatus = CompensationStatus.Completed;
                            allCompensated = true;
                        }
                        else
                        {
                            reserveStep.CompensationStatus = CompensationStatus.Failed;
                            reserveStep.ErrorMessage = result.ErrorMessage;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to compensate saga {SagaId}", saga.Id);
                        reserveStep.CompensationStatus = CompensationStatus.Failed;
                    }
                }
            }

            if (allCompensated)
            {
                saga.Status = SagaStatus.Compensated;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
