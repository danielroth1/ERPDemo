namespace ERP.Contracts;

public static class KafkaTopics
{
    // Saga trigger topics (single message type per topic)
    public const string SubmitPurchase = "submit-purchase";
    public const string SubmitReturn = "submit-return";

    // Inventory command topics (one per command type)
    public const string ReserveStockCommand = "reserve-stock";
    public const string DeductStockCommand = "deduct-stock";
    public const string RestoreStockCommand = "restore-stock";

    // Financial command topics (one per command type)
    public const string CreatePurchaseTransactionCommand = "create-purchase-transaction";
    public const string CreateRefundTransactionCommand = "create-refund-transaction";
    public const string VoidPurchaseTransactionCommand = "void-purchase-transaction";

    // Purchase saga event topics (one per event type)
    public const string StockReservedEvent = "stock-reserved";
    public const string StockReservationFailedEvent = "stock-reservation-failed";
    public const string StockDeductedEvent = "stock-deducted";
    public const string StockDeductionFailedEvent = "stock-deduction-failed";
    public const string PurchaseTransactionCreatedEvent = "purchase-transaction-created";
    public const string PurchaseTransactionFailedEvent = "purchase-transaction-failed";

    // Return saga event topics (one per event type)
    public const string StockRestoredEvent = "stock-restored";
    public const string RefundTransactionCreatedEvent = "refund-transaction-created";
    public const string RefundTransactionFailedEvent = "refund-transaction-failed";

    // Saga result topics (one per result type)
    public const string PurchaseCompletedEvent = "purchase-completed";
    public const string PurchaseFailedEvent = "purchase-failed";
    public const string ReturnCompletedEvent = "return-completed";
    public const string ReturnFailedEvent = "return-failed";

    // Inventory domain event topics (one per event type)
    public const string ProductCreatedEvent = "product-created";
    public const string ProductUpdatedEvent = "product-updated";
    public const string ProductDeletedEvent = "product-deleted";
    public const string StockUpdatedEvent = "stock-updated";
    public const string LowStockAlertEvent = "low-stock-alert";
    public const string StockMovementCreatedEvent = "stock-movement-created";

    // User domain event topics (one per event type)
    public const string UserCreatedEvent = "user-created";
    public const string UserUpdatedEvent = "user-updated";
    public const string UserDeletedEvent = "user-deleted";
    public const string UserDeactivatedEvent = "user-deactivated";

    // Sales domain event topics (one per event type)
    public const string OrderCreatedEvent = "order-created";
    public const string OrderStatusChangedEvent = "order-status-changed";
    public const string InvoiceCreatedEvent = "invoice-created";
    public const string InvoicePaidEvent = "invoice-paid";

    // Financial domain event topics (one per event type)
    public const string TransactionCreatedEvent = "transaction-created";
    public const string BudgetExceededEvent = "budget-exceeded";
}
