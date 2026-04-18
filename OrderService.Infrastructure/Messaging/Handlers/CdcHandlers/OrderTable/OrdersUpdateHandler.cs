//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using OrderService.Domain.Entities;
//using OrderService.Domain.ReadModels;
//using OrderService.Infrastructure.Data;
//using System.Text.Json;

//namespace OrderService.Infrastructure.Messaging.Handlers.CdcHandlers.OrderTable;

//public class OrdersUpdateHandler : ICdcEventHandler
//{
//    private readonly ILogger<OrdersUpdateHandler> _logger;
//    private readonly IServiceProvider _serviceProvider;

//    public string TableName => "orders";
//    public string Operation => CdcOperations.Update;

//    public OrdersUpdateHandler(
//  ILogger<OrdersUpdateHandler> logger,
//        IServiceProvider serviceProvider)
//    {
//        _logger = logger;
//        _serviceProvider = serviceProvider;
//    }

//    public async Task HandleAsync(string eventData, CancellationToken cancellationToken)
//    {
//        try
//        {
//            using var jsonDoc = JsonDocument.Parse(eventData);
//            var root = jsonDoc.RootElement;
//            JsonElement payloadElement;
//            if (root.TryGetProperty("payload", out var payload))
//            {
//                payloadElement = payload;
//            }
//            else
//            {
//                payloadElement = root;
//            }
//            var cdcEvent = JsonSerializer.Deserialize<CdcEvent<OrderCdcData>>(payloadElement.GetRawText());

//            if (cdcEvent?.After == null)
//            {
//                _logger.LogWarning("UPDATE event has no 'after' data. Skipping.");
//                return;
//            }

//            var data = cdcEvent.After;
//            _logger.LogDebug(
//         "Raw CDC data - Id: {Id}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}, Status: {Status}",
//          data.Id, data.CustomerId, data.TotalAmount, data.Status);
//            try
//            {
//                data.Validate();
//            }
//            catch (ArgumentException ex)
//            {
//                _logger.LogError(ex, "CDC data validation failed. Skipping invalid event.");
//                return;
//            }
//            Guid orderId;
//            Guid customerId;
//            decimal totalAmount;
//            DateTime? modifiedAt;

//            try
//            {
//                orderId = data.GetIdAsGuid();
//                customerId = data.GetCustomerIdAsGuid();
//                totalAmount = data.GetTotalAmountAsDecimal();
//                modifiedAt = data.GetModifiedAsDateTime();
//            }
//            catch (ArgumentException ex)
//            {
//                _logger.LogError(ex, "Failed to convert CDC data types. Raw data: Id={Id}, CustomerId={CustomerId}, TotalAmount={TotalAmount}",
//            data.Id, data.CustomerId, data.TotalAmount);
//                throw;
//            }

//            _logger.LogInformation(
//       "Processing UPDATE for Order {OrderId}, Customer: {CustomerId}, New Amount: {TotalAmount}",
//         orderId, customerId, totalAmount);
//            using var scope = _serviceProvider.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var existing = await dbContext.OrderReadModels
//        .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

//            if (existing == null)
//            {
//                _logger.LogWarning("Order {OrderId} not found in read DB. Creating new entry from UPDATE event.", orderId);

//                var readModel = new OrderReadModel
//                {
//                    Id = orderId,
//                    CustomerId = customerId,
//                    TotalAmount = totalAmount,
//                    Status = (OrderStatus)data.Status,
//                    CreatedAt = data.GetCreatedAsDateTime(),
//                    Modified = modifiedAt
//                };

//                await dbContext.OrderReadModels.AddAsync(readModel, cancellationToken);
//            }
//            else
//            {
//                existing.CustomerId = customerId;
//                existing.TotalAmount = totalAmount;
//                existing.Status = (OrderStatus)data.Status;
//                existing.Modified = modifiedAt ?? DateTime.UtcNow;

//                dbContext.OrderReadModels.Update(existing);
//            }

//            await dbContext.SaveChangesAsync(cancellationToken);

//            _logger.LogInformation(
//            "Successfully synced UPDATE for Order {OrderId} to read DB (New Amount: {Amount})",
//            orderId, totalAmount);
//        }
//        catch (JsonException ex)
//        {
//            _logger.LogError(ex, "Failed to parse CDC event JSON");
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error handling UPDATE event for orders table");
//            throw;
//        }
//    }
//}