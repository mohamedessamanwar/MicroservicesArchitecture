using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.RabbitImplementation.Inbox;

public sealed class InboxStore : IInboxStore
{
    private readonly AppDbContext _dbContext;

    public InboxStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        await _dbContext.InboxMessages.AddAsync(message, cancellationToken);
    }
}

