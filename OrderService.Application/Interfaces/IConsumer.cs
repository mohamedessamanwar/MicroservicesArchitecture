namespace OrderService.Application.Interfaces;

/// <summary>
/// Contract between the messaging consumer job and your domain handler (one consumer class per event type).
/// The job resolves <see cref="IConsumer{TMessage}"/> from DI and calls <see cref="ConsumeAsync"/> after inbox de-duplication.
/// </summary>
public interface IConsumer<in TMessage>
{
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken = default);
}
