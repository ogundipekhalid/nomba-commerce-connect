namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// A line item within an order. Price and vendor are snapshotted at order time so that
/// later product edits (price changes, deactivation) never retroactively change a
/// placed order.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;

    public Guid VendorId { get; private set; }
    public string VendorNombaAccountId { get; private set; } = default!;

    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem() { }

    public OrderItem(Product product, int quantity)
    {
        if (product is null) throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (product.Vendor is null) throw new InvalidOperationException("Product must have its Vendor loaded to snapshot the Nomba account id.");

        Id = Guid.NewGuid();
        ProductId = product.Id;
        ProductName = product.Name;
        VendorId = product.VendorId;
        VendorNombaAccountId = product.Vendor.NombaAccountId;
        UnitPrice = product.Price;
        Quantity = quantity;
    }

    /// <summary>Used by EF Core / repositories when reconstructing from persistence.</summary>
    internal void AttachToOrder(Guid orderId) => OrderId = orderId;
}
