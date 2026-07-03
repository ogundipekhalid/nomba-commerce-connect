using Microsoft.EntityFrameworkCore;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(builder =>
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.BusinessName).IsRequired().HasMaxLength(200);
            builder.Property(v => v.Email).IsRequired().HasMaxLength(200);
            builder.Property(v => v.NombaAccountId).IsRequired().HasMaxLength(100);
            builder.HasIndex(v => v.Email).IsUnique();
            builder.HasMany(v => v.Products)
                .WithOne(p => p.Vendor)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
            builder.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.OrderReference).IsRequired().HasMaxLength(60);
            builder.HasIndex(o => o.OrderReference).IsUnique();
            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);

            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order.TotalAmount is a computed property, not a persisted column.
            builder.Ignore(o => o.TotalAmount);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.VendorNombaAccountId).IsRequired().HasMaxLength(100);
            builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Ignore(i => i.LineTotal);
        });

        modelBuilder.Entity<PaymentTransaction>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.EventType).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
            builder.HasIndex(t => t.NombaRequestId);
        });
    }
}
