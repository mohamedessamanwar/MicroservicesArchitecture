//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using OrderService.Infrastructure.Data;
//using System.Text.Json;

//namespace OrderService.Infrastructure.Messaging.Handlers.CdcHandlers.OrderTable;

//public class OrdersDeleteHandler : ICdcEventHandler
//{
//    private readonly ILogger<OrdersDeleteHandler> _logger;
//    private readonly IServiceProvider _serviceProvider;

//    public string TableName => "orders";
//    public string Operation => CdcOperations.Delete;

//    public OrdersDeleteHandler(
//        ILogger<OrdersDeleteHandler> logger,
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

//            if (cdcEvent?.Before == null)
//            {
//                _logger.LogWarning("DELETE event has no 'before' data. Skipping.");
//                return;
//            }

//            var data = cdcEvent.Before;
//            _logger.LogDebug("Raw CDC data - Id: {Id}", data.Id);
//            if (string.IsNullOrWhiteSpace(data.Id))
//            {
//                _logger.LogError("DELETE event has null or empty ID. Skipping.");
//                return;
//            }
//            Guid orderId;

//            try
//            {
//                orderId = data.GetIdAsGuid();
//            }
//            catch (ArgumentException ex)
//            {
//                _logger.LogError(ex, "Failed to convert ID to Guid. Raw Id: {Id}", data.Id);
//                throw;
//            }

//            _logger.LogInformation(
//                "Processing DELETE for Order {OrderId}",
//                orderId);
//            using var scope = _serviceProvider.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//          //  var existing = await dbContext.OrderReadModels
//                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

//            if (existing == null)
//            {
//                _logger.LogWarning("Order {OrderId} not found in read DB. Nothing to delete.", orderId);
//                return;
//            }

//          //  dbContext.OrderReadModels.Remove(existing);
//            await dbContext.SaveChangesAsync(cancellationToken);

//            _logger.LogInformation("Successfully synced DELETE for Order {OrderId} from read DB", orderId);
//        }
//        catch (JsonException ex)
//        {
//            _logger.LogError(ex, "Failed to parse CDC event JSON");
//            throw;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error handling DELETE event for orders table");
//            throw;
//        }
//    }
//}