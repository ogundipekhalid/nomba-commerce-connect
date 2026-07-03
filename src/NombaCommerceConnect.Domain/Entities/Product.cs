namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// A product listed for sale by a vendor.
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public Vendor? Vendor { get; private set; }

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private Product() { }

    public Product(Guid vendorId, string name, string description, decimal price, int stockQuantity, string? imageUrl)
    {
        if (vendorId == Guid.Empty)
            throw new ArgumentException("A product must belong to a vendor.", nameof(vendorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock cannot be negative.");

        Id = Guid.NewGuid();
        VendorId = vendorId;
        Name = name;
        Description = description ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
        ImageUrl = imageUrl;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock for product '{Name}'. Available: {StockQuantity}, requested: {quantity}.");

        StockQuantity -= quantity;
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        StockQuantity += quantity;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Associates the vendor navigation in-memory. EF Core populates this
    /// automatically via <c>Include(p => p.Vendor)</c>; this method exists so the
    /// same wiring can be done explicitly in unit tests and seed/demo data that
    /// don't go through the database.
    /// </summary>
    public void AttachVendor(Vendor vendor) => Vendor = vendor;
}
