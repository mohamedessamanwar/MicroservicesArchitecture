namespace OrderService.Domain.Events
{
    public abstract class BaseEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; }

        protected BaseEvent(string eventType)
        {
            EventType = eventType;
        }
    }
}