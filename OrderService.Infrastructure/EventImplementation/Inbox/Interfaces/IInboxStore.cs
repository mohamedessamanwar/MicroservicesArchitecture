using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.EventImplementation.Inbox;

public interface IInboxStore
{
    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);
}

