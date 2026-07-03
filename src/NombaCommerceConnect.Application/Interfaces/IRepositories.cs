using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Application.Interfaces;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Vendor vendor, CancellationToken ct = default);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Fetches products including their Vendor navigation loaded (needed to snapshot NombaAccountId on order items).</summary>
    Task<IReadOnlyList<Product>> GetByIdsWithVendorAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByOrderReferenceAsync(string orderReference, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);

    /// <summary>Used by the webhook handler to detect and skip already-processed deliveries.</summary>
    Task<bool> ExistsByRequestIdAsync(string nombaRequestId, CancellationToken ct = default);
}

/// <summary>
/// Persists changes made through repositories in a single transaction/unit.
/// With EF Core this simply wraps <c>SaveChangesAsync</c>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
