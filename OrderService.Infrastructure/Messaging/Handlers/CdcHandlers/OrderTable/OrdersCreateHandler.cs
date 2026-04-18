//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using OrderService.Domain.Entities;
//using OrderService.Domain.ReadModels;
//using OrderService.Infrastructure.Data;
//using System.Text.Json;

//namespace OrderService.Infrastructure.Messaging.Handlers.CdcHandlers.OrderTable;

//public class OrdersCreateHandler : ICdcEventHandler
//{
//    private readonly ILogger<OrdersCreateHandler> _logger;
//    private readonly IServiceProvider _serviceProvider;

//    public string TableName => "orders";
//    public string Operation => CdcOperations.Create;

//    public OrdersCreateHandler(
//        ILogger<OrdersCreateHandler> logger,
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
//                _logger.LogWarning("CREATE event has no 'after' data. Skipping.");
//                return;
//            }

//            var data = cdcEvent.After;
//            _logger.LogDebug(
//                "Raw CDC data - Id: {Id}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}, Status: {Status}, Created: {Created}",
//                data.Id, data.CustomerId, data.TotalAmount, data.Status, data.Created);
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
//            DateTime createdAt;

//            try
//            {
//                orderId = data.GetIdAsGuid();
//                customerId = data.GetCustomerIdAsGuid();
//                totalAmount = data.GetTotalAmountAsDecimal();
//                createdAt = data.GetCreatedAsDateTime();
//            }
//            catch (ArgumentException ex)
//            {
//                _logger.LogError(ex, "Failed to convert CDC data types. Raw data: Id={Id}, CustomerId={CustomerId}, TotalAmount={TotalAmount}",
//                    data.Id, data.CustomerId, data.TotalAmount);
//                throw;
//            }

//            _logger.LogInformation(
//                "Processing CREATE for Order {OrderId}, Customer: {CustomerId}, Amount: {TotalAmount}",
//                orderId, customerId, totalAmount);
//            using var scope = _serviceProvider.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var existing = await dbContext.OrderReadModels
//                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

//            if (existing != null)
//            {
//                _logger.LogInformation("Order {OrderId} already exists in read DB. Skipping CREATE.", orderId);
//                return;
//            }
//            var readModel = new OrderReadModel
//            {
//                Id = orderId,
//                CustomerId = customerId,
//                TotalAmount = totalAmount,
//                Status = (OrderStatus)data.Status,
//                CreatedAt = createdAt,
//                Modified = data.GetModifiedAsDateTime()
//            };

//            await dbContext.OrderReadModels.AddAsync(readModel, cancellationToken);
//            await dbContext.SaveChangesAsync(cancellationToken);

//            _logger.LogInformation(
//                "Successfully synced CREATE for Order {OrderId} to read DB (Amount: {Amount})",
//                orderId, totalAmount);
//        }
//        catch (JsonException ex)
//        {
//            _logger.LogError(ex, "Failed to parse CDC event JSON. Event data: {EventData}",
//                eventData.Length > 500 ? eventData.Substring(0, 500) + "..." : eventData);
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error handling CREATE event for orders table");
//            throw;
//        }
//    }
//}