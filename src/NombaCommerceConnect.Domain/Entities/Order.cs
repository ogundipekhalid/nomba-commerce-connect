using NombaCommerceConnect.Domain.Enums;

namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// An order placed by a customer. Acts as the aggregate root for its line items and
/// owns the business rules for status transitions and vendor payout splitting.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Unique reference sent to Nomba as `orderReference` on checkout order creation,
    /// and echoed back in webhooks. Also used to correlate our DB row to Nomba's records.
    /// </summary>
    public string OrderReference { get; private set; } = default!;

    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public string Currency { get; private set; } = "NGN";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    /// <summary>Nomba transaction id, populated once a webhook/verification confirms payment.</summary>
    public string? NombaTransactionId { get; private set; }

    /// <summary>Checkout link returned by Nomba, kept for reference/debugging.</summary>
    public string? CheckoutLink { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    private Order() { }

    public static Order Create(Guid customerId, IEnumerable<OrderItem> items)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("An order must belong to a customer.", nameof(customerId));

        var itemList = items?.ToList() ?? new List<OrderItem>();
        if (itemList.Count == 0)
            throw new InvalidOperationException("An order must have at least one item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderReference = $"NCC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..40],
            Status = OrderStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var item in itemList)
        {
            item.AttachToOrder(order.Id);
            order._items.Add(item);
        }

        return order;
    }

    /// <summary>
    /// Computes each vendor's share of the order as a percentage (0-100, rounded to 2dp,
    /// with any rounding remainder assigned to the largest share) suitable for building
    /// a Nomba checkout `splitRequest.splitList`.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> GetVendorSplitPercentages()
    {
        var total = TotalAmount;
        if (total <= 0)
            throw new InvalidOperationException("Cannot compute a split for an order with zero total.");

        var byVendor = _items
            .GroupBy(i => i.VendorNombaAccountId)
            .Select(g => new { NombaAccountId = g.Key, Amount = g.Sum(i => i.LineTotal) })
            .OrderByDescending(g => g.Amount)
            .ToList();

        var result = new Dictionary<string, decimal>();
        decimal runningTotal = 0m;

        for (var i = 0; i < byVendor.Count; i++)
        {
            var entry = byVendor[i];
            decimal percentage;

            if (i == byVendor.Count - 1)
            {
                // last entry absorbs rounding remainder so percentages sum to exactly 100
                percentage = Math.Round(100m - runningTotal, 2);
            }
            else
            {
                percentage = Math.Round(entry.Amount / total * 100m, 2);
                runningTotal += percentage;
            }

            result[entry.NombaAccountId] = percentage;
        }

        return result;
    }

    public void MarkPendingPayment(string checkoutLink)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException($"Cannot move order from {Status} to {OrderStatus.PendingPayment}.");
        if (string.IsNullOrWhiteSpace(checkoutLink))
            throw new ArgumentException("Checkout link is required.", nameof(checkoutLink));

        CheckoutLink = checkoutLink;
        Status = OrderStatus.PendingPayment;
    }

    public void MarkPaid(string nombaTransactionId)
    {
        if (Status is OrderStatus.Paid)
            return; // idempotent - webhook or manual re-verify may fire more than once

        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException($"Cannot mark order as paid from status {Status}.");
        if (string.IsNullOrWhiteSpace(nombaTransactionId))
            throw new ArgumentException("Transaction id is required.", nameof(nombaTransactionId));

        NombaTransactionId = nombaTransactionId;
        Status = OrderStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status is OrderStatus.Paid)
            throw new InvalidOperationException("Cannot mark a paid order as failed.");

        Status = OrderStatus.Failed;
    }

    public void MarkRefunded(bool isFullRefund)
    {
        if (Status != OrderStatus.Paid && Status != OrderStatus.PartiallyRefunded)
            throw new InvalidOperationException($"Cannot refund an order in status {Status}.");

        Status = isFullRefund ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
    }
}
