namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// A merchant selling products through the marketplace. Each vendor is expected to
/// hold their own Nomba sub-account so that split payments can route their share of
/// a sale directly to them at checkout time.
/// </summary>
public class Vendor
{
    public Guid Id { get; private set; }
    public string BusinessName { get; private set; } = default!;
    public string Email { get; private set; } = default!;

    /// <summary>
    /// The vendor's Nomba sub-account identifier. Required for split payments.
    /// This is provisioned on the Nomba dashboard, not created by this system.
    /// </summary>
    public string NombaAccountId { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Vendor() { }

    public Vendor(string businessName, string email, string nombaAccountId)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required.", nameof(businessName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(nombaAccountId))
            throw new ArgumentException("Nomba sub-account id is required for split payouts.", nameof(nombaAccountId));

        Id = Guid.NewGuid();
        BusinessName = businessName;
        Email = email;
        NombaAccountId = nombaAccountId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
