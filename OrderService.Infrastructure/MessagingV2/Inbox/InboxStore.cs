using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.MessagingV2.Inbox;

public sealed class InboxStore : IInboxStore
{
    private readonly AppDbContext _dbContext;

    public InboxStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return _dbContext.InboxMessages.AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        await _dbContext.InboxMessages.AddAsync(message, cancellationToken);
    }
}
