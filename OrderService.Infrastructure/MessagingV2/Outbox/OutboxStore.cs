using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.MessagingV2.Outbox;

public sealed class OutboxStore : IOutboxStore
{
    private readonly AppDbContext _dbContext;

    public OutboxStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        return await _dbContext.OutboxMessages
            .Where(x => x.Status == MessagingStatusConstants.Pending)
            .OrderBy(x => x.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Marks tracked message as sent. Message must already be tracked by DbContext.
    /// No database query required; object is already in memory from GetPendingBatchAsync.
    /// </summary>
    public void MarkAsSent(OutboxMessage message, DateTime processedOnUtc)
    {
        message.Status = MessagingStatusConstants.Sent;
        message.ProcessedOnUtc = processedOnUtc;
        message.LastError = null;
    }

    /// <summary>
    /// Marks tracked message as failed. Message must already be tracked by DbContext.
    /// No database query required; object is already in memory from GetPendingBatchAsync.
    /// </summary>
    public void MarkAsFailed(OutboxMessage message, string error, int retryCount)
    {
        message.Status = MessagingStatusConstants.Failed;
        message.LastError = error;
        message.RetryCount = retryCount;
    }

    /// <summary>
    /// Saves all tracked changes for the given messages in a single batch operation.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
