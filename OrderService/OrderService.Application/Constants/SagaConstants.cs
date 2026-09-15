namespace OrderService.Application.Constants;

public static class SagaConstants
{
    public static class Types
    {
        public const string CreateOrderSaga = "CreateOrderSaga";
    }

    public static class Steps
    {
        public const string ReserveInventory = "ReserveInventory";
        public const string ChargePayment = "ChargePayment";
        public const string Completed = "Completed";
    }
}
