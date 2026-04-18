using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.MessagingV2.Outbox;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates a tracked message object (obtained from GetPendingBatchAsync) as sent.
    /// Does not query database; object is already tracked by DbContext.
    /// Call SaveChangesAsync() after updating all messages to save in batch.
    /// </summary>
    void MarkAsSent(OutboxMessage message, DateTime processedOnUtc);
    
    /// <summary>
    /// Updates a tracked message object (obtained from GetPendingBatchAsync) as failed.
    /// Does not query database; object is already tracked by DbContext.
    /// Call SaveChangesAsync() after updating all messages to save in batch.
    /// </summary>
    void MarkAsFailed(OutboxMessage message, string error, int retryCount);
    
    /// <summary>
    /// Saves all tracked changes (from MarkAsSent/MarkAsFailed calls) in a single batch operation.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

