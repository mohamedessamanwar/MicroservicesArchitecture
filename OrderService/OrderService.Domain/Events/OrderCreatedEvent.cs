namespace OrderService.Domain.Events
{
    public class OrderCreatedEvent : BaseEvent
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }

        public OrderCreatedEvent(Guid orderId, Guid customerId, decimal totalAmount) : base(nameof(OrderCreatedEvent))
        {
            OrderId = orderId;
            CustomerId = customerId;
            TotalAmount = totalAmount;
        }
        public OrderCreatedEvent() : base(nameof(OrderCreatedEvent)) { }
    }
}