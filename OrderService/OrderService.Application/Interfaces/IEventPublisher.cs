namespace OrderService.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    }
}