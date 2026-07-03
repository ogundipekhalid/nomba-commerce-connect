namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// The buyer placing an order. Kept intentionally minimal for this build phase -
/// no auth/identity provider is wired up yet, customers are picked/created by email.
/// </summary>
public class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private Customer() { }

    public Customer(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Id = Guid.NewGuid();
        FullName = fullName;
        Email = email;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
