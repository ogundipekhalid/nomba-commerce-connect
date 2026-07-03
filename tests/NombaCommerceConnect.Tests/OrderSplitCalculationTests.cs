using NombaCommerceConnect.Domain.Entities;
using Xunit;

namespace NombaCommerceConnect.Tests;

public class OrderSplitCalculationTests
{
    private static Vendor MakeVendor(string nombaAccountId) =>
        new($"Vendor {nombaAccountId}", $"{nombaAccountId}@example.com", nombaAccountId);

    private static Product MakeProduct(Vendor vendor, decimal price, int stock = 100)
    {
        var product = new Product(vendor.Id, $"Product-{Guid.NewGuid():N}", "desc", price, stock, null);
        product.AttachVendor(vendor);
        return product;
    }

    [Fact]
    public void SingleVendorOrder_GetsFullOnHundredPercentSplit()
    {
        var vendor = MakeVendor("acc-1");
        var product = MakeProduct(vendor, 5000m);
        var item = new OrderItem(product, 2);

        var order = Order.Create(Guid.NewGuid(), new[] { item });
        var splits = order.GetVendorSplitPercentages();

        Assert.Single(splits);
        Assert.Equal(100m, splits["acc-1"]);
    }

    [Fact]
    public void TwoVendorOrder_SplitsProportionallyAndSumsToOneHundred()
    {
        var vendorA = MakeVendor("acc-a");
        var vendorB = MakeVendor("acc-b");

        var productA = MakeProduct(vendorA, 3000m); // 3000
        var productB = MakeProduct(vendorB, 1000m); // 1000, total 4000 -> 75% / 25%

        var items = new[]
        {
            new OrderItem(productA, 1),
            new OrderItem(productB, 1)
        };

        var order = Order.Create(Guid.NewGuid(), items);
        var splits = order.GetVendorSplitPercentages();

        Assert.Equal(2, splits.Count);
        Assert.Equal(75m, splits["acc-a"]);
        Assert.Equal(25m, splits["acc-b"]);
        Assert.Equal(100m, splits.Values.Sum());
    }

    [Fact]
    public void ThreeVendorOrder_RoundingRemainderIsAbsorbedAndStillSumsToOneHundred()
    {
        // Amounts chosen to force a non-terminating percentage (1/3 each).
        var vendorA = MakeVendor("acc-a");
        var vendorB = MakeVendor("acc-b");
        var vendorC = MakeVendor("acc-c");

        var productA = MakeProduct(vendorA, 1000m);
        var productB = MakeProduct(vendorB, 1000m);
        var productC = MakeProduct(vendorC, 1000m);

        var items = new[]
        {
            new OrderItem(productA, 1),
            new OrderItem(productB, 1),
            new OrderItem(productC, 1)
        };

        var order = Order.Create(Guid.NewGuid(), items);
        var splits = order.GetVendorSplitPercentages();

        Assert.Equal(3, splits.Count);
        Assert.Equal(100m, splits.Values.Sum());
    }

    [Fact]
    public void SameVendorAcrossMultipleItems_IsMergedIntoOneSplitEntry()
    {
        var vendor = MakeVendor("acc-1");
        var productA = MakeProduct(vendor, 2000m);
        var productB = MakeProduct(vendor, 3000m);

        var items = new[]
        {
            new OrderItem(productA, 1),
            new OrderItem(productB, 1)
        };

        var order = Order.Create(Guid.NewGuid(), items);
        var splits = order.GetVendorSplitPercentages();

        Assert.Single(splits);
        Assert.Equal(100m, splits["acc-1"]);
    }

    [Fact]
    public void OrderItem_Construction_RequiresProductVendorToBeLoaded()
    {
        var vendorlessProduct = new Product(Guid.NewGuid(), "Orphan", "desc", 100m, 10, null);

        Assert.Throws<InvalidOperationException>(() => new OrderItem(vendorlessProduct, 1));
    }
}
