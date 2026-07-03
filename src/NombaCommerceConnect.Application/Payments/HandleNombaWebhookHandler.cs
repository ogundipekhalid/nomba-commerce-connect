using System.Text.Json;
using Microsoft.Extensions.Logging;
using NombaCommerceConnect.Application.Common;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Application.Nomba.Models;
using NombaCommerceConnect.Domain.Entities;
using NombaCommerceConnect.Domain.Enums;

namespace NombaCommerceConnect.Application.Payments;

/// <summary>
/// Processes an inbound Nomba webhook delivery. Follows Nomba's documented guidance:
/// verify the signature, never trust the payload alone for something as important as
/// "did we get paid" - re-check the transaction status via the API - and treat webhook
/// delivery as at-least-once (duplicates are expected and must be handled idempotently).
/// </summary>
public class HandleNombaWebhookHandler
{
    private readonly INombaWebhookSignatureVerifier _signatureVerifier;
    private readonly INombaClient _nombaClient;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleNombaWebhookHandler> _logger;

    public HandleNombaWebhookHandler(
        INombaWebhookSignatureVerifier signatureVerifier,
        INombaClient nombaClient,
        IOrderRepository orderRepository,
        IPaymentTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ILogger<HandleNombaWebhookHandler> logger)
    {
        _signatureVerifier = signatureVerifier;
        _nombaClient = nombaClient;
        _orderRepository = orderRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(string rawBody, string? signatureHeaderValue, CancellationToken ct = default)
    {
        if (!_signatureVerifier.IsValid(rawBody, signatureHeaderValue))
        {
            _logger.LogWarning("Rejected a Nomba webhook with an invalid signature.");
            return Result.Failure("Invalid webhook signature.", "INVALID_SIGNATURE");
        }

        NombaWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NombaWebhookPayload>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Nomba webhook payload.");
            return Result.Failure("Malformed webhook payload.", "MALFORMED_PAYLOAD");
        }

        if (payload?.Data?.Order is null || payload.Data.Transaction is null)
            return Result.Failure("Webhook payload missing required order/transaction data.", "MALFORMED_PAYLOAD");

        // Idempotency: Nomba (like most payment providers) may deliver the same event
        // more than once. If we've already recorded this requestId, acknowledge and stop.
        if (!string.IsNullOrWhiteSpace(payload.RequestId) &&
            await _transactionRepository.ExistsByRequestIdAsync(payload.RequestId, ct))
        {
            _logger.LogInformation("Duplicate Nomba webhook delivery ignored: {RequestId}", payload.RequestId);
            return Result.Success();
        }

        var order = await _orderRepository.GetByOrderReferenceAsync(payload.Data.Order.OrderReference, ct);
        if (order is null)
        {
            _logger.LogWarning("Nomba webhook referenced an unknown order: {OrderReference}", payload.Data.Order.OrderReference);
            return Result.Failure("Unknown order reference.", "ORDER_NOT_FOUND");
        }

        // Never trust the webhook body alone - re-verify the transaction status directly
        // against Nomba's API before marking anything as paid.
        TransactionStatusResult verification;
        try
        {
            verification = await _nombaClient.VerifyTransactionAsync(order.OrderReference, ct);
        }
        catch (NombaApiException ex)
        {
            _logger.LogError(ex, "Failed to re-verify transaction for order {OrderReference} after webhook.", order.OrderReference);
            return Result.Failure($"Could not verify transaction with Nomba: {ex.Message}", ex.NombaCode ?? "NOMBA_ERROR");
        }

        var status = verification.IsSuccessful ? PaymentStatus.Success : PaymentStatus.Failed;

        var record = PaymentTransaction.FromWebhook(
            orderId: order.Id,
            eventType: payload.EventType,
            nombaTransactionId: payload.Data.Transaction.TransactionId,
            nombaRequestId: payload.RequestId,
            amount: payload.Data.Order.Amount,
            status: status,
            rawPayload: rawBody);

        await _transactionRepository.AddAsync(record, ct);

        if (verification.IsSuccessful)
        {
            order.MarkPaid(verification.TransactionId);
            await _orderRepository.UpdateAsync(order, ct);
        }
        else
        {
            _logger.LogInformation("Webhook for order {OrderReference} did not correspond to a successful transaction (status: {Status}).",
                order.OrderReference, verification.Status);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
