using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.RabbitImplementation.Inbox;

public interface IInboxStore
{
    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);
}

