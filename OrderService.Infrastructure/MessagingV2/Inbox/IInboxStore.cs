using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.MessagingV2.Inbox;

public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken);
    Task AddAsync(InboxMessage message, CancellationToken cancellationToken);
}
