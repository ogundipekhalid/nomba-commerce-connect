using Microsoft.EntityFrameworkCore;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Api;

/// <summary>
/// Dev-only convenience seeding so a judge or teammate running <c>dotnet run</c> for
/// the first time sees a populated storefront instead of an empty product list. Uses
/// placeholder Nomba sub-account ids (see README "Assumptions") since real vendor
/// onboarding to Nomba is outside this build phase's scope.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedIfEmptyAsync(Infrastructure.Persistence.AppDbContext db)
    {
        if (await db.Vendors.AnyAsync())
            return;

        var vendorOne = new Vendor("Lagos Threads", "vendor1@example.com", "SEED-ACCOUNT-1");
        var vendorTwo = new Vendor("Naija Gadgets", "vendor2@example.com", "SEED-ACCOUNT-2");
        db.Vendors.AddRange(vendorOne, vendorTwo);

        var products = new[]
        {
            new Product(vendorOne.Id, "Ankara Print Shirt", "Handmade Ankara shirt, breathable cotton blend.", 8500m, 25,
                "https://placehold.co/300x200?text=Ankara+Shirt"),
            new Product(vendorOne.Id, "Agbada Set", "Traditional 3-piece agbada, made to order.", 45000m, 10,
                "https://placehold.co/300x200?text=Agbada+Set"),
            new Product(vendorTwo.Id, "Wireless Earbuds", "Bluetooth 5.3 earbuds with charging case.", 15000m, 40,
                "https://placehold.co/300x200?text=Earbuds"),
            new Product(vendorTwo.Id, "Power Bank 20000mAh", "Fast-charging power bank, dual USB-C.", 12000m, 30,
                "https://placehold.co/300x200?text=Power+Bank"),
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
