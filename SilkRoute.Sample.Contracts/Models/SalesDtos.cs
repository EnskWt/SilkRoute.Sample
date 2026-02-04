namespace SilkRoute.Sample.Contracts.Models;

public sealed class CreateOrderRequest
{
    public Guid CustomerId { get; init; }
    public List<OrderLineDto> Lines { get; init; } = new();
}

public sealed class OrderLineDto
{
    public string Sku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class PlaceOrderResponse
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Currency { get; init; } = "USD";
}