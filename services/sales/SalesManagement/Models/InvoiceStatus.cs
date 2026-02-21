namespace SalesManagement.Models;

public enum InvoiceStatus
{
    Draft,
    Pending,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}
