using Microsoft.Extensions.Logging;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Application.Nomba.Models;

namespace NombaCommerceConnect.Infrastructure.Payments.Nomba;

/// <summary>
/// Stand-in for <see cref="NombaClient"/> used while waiting on live Nomba API
/// credentials. Returns responses shaped exactly like Nomba's documented ones so the
/// rest of the system (order flow, webhook handling once triggered manually,
/// refunds) can be built, tested, and demoed end-to-end before credentials arrive.
///
/// Swap this out for the real <see cref="NombaClient"/> by setting
/// <c>Nomba:UseFakeClient</c> to <c>false</c> once credentials are available - see
/// README for details. This class is registered instead of the real client, never
/// alongside it.
/// </summary>
public class FakeNombaClient : INombaClient
{
    private readonly ILogger<FakeNombaClient> _logger;

    public FakeNombaClient(ILogger<FakeNombaClient> logger)
    {
        _logger = logger;
    }

    public Task<CheckoutOrderResult> CreateCheckoutOrderAsync(CreateCheckoutOrderRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[FakeNombaClient] Simulated checkout order created for {OrderReference}, amount {Amount} {Currency}, split across {SplitCount} vendor(s).",
            request.OrderReference, request.Amount, request.Currency, request.SplitRequest?.SplitList.Count ?? 1);

        var fakeLink = $"https://sandbox.nomba.com/pay/fake-{request.OrderReference}";

        return Task.FromResult(new CheckoutOrderResult
        {
            CheckoutLink = fakeLink,
            OrderReference = request.OrderReference
        });
    }

    public Task<TransactionStatusResult> VerifyTransactionAsync(string orderReference, CancellationToken ct = default)
    {
        _logger.LogInformation("[FakeNombaClient] Simulated transaction verification for {OrderReference} - reporting SUCCESS.", orderReference);

        return Task.FromResult(new TransactionStatusResult
        {
            TransactionId = $"FAKE-TXN-{orderReference}",
            OrderReference = orderReference,
            Status = "SUCCESS",
            Amount = 0m
        });
    }

    public Task<RefundResult> RefundTransactionAsync(RefundRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("[FakeNombaClient] Simulated refund for transaction {TransactionId}, amount {Amount}.",
            request.TransactionId, request.Amount);

        return Task.FromResult(new RefundResult
        {
            Success = true,
            Message = "Simulated refund - no live Nomba credentials configured yet.",
            RefundReference = $"FAKE-REFUND-{Guid.NewGuid():N}"
        });
    }
}
