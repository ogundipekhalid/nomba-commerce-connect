using Microsoft.EntityFrameworkCore;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Infrastructure.Persistence.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _db;
    public VendorRepository(AppDbContext db) => _db = db;

    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Vendors.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Vendor vendor, CancellationToken ct = default) =>
        await _db.Vendors.AddAsync(vendor, ct);
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Products.Include(p => p.Vendor).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsWithVendorAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _db.Products
            .Include(p => p.Vendor)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.Products.AsNoTracking().Include(p => p.Vendor).Where(p => p.IsActive).ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default) =>
        await _db.Customers.AddAsync(customer, ct);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByOrderReferenceAsync(string orderReference, CancellationToken ct = default) =>
        _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderReference == orderReference, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _db.Orders.AddAsync(order, ct);

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }
}

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly AppDbContext _db;
    public PaymentTransactionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default) =>
        await _db.PaymentTransactions.AddAsync(transaction, ct);

    public Task<bool> ExistsByRequestIdAsync(string nombaRequestId, CancellationToken ct = default) =>
        _db.PaymentTransactions.AnyAsync(t => t.NombaRequestId == nombaRequestId, ct);
}

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public EfUnitOfWork(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
