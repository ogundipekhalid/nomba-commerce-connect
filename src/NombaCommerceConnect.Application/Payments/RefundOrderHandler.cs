using Microsoft.Extensions.Logging;
using NombaCommerceConnect.Application.Common;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Application.Nomba.Models;
using NombaCommerceConnect.Domain.Entities;
using NombaCommerceConnect.Domain.Enums;

namespace NombaCommerceConnect.Application.Payments;

public class RefundOrderRequest
{
    public required Guid OrderId { get; init; }

    /// <summary>Null issues a full refund of the order total.</summary>
    public decimal? Amount { get; init; }
    public string? Reason { get; init; }
}

public class RefundOrderResult
{
    public required Guid OrderId { get; init; }
    public required bool IsFullRefund { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Lets a vendor/admin issue a refund against a paid order. Only orders in a Paid or
/// already PartiallyRefunded state are eligible.
/// </summary>
public class RefundOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INombaClient _nombaClient;
    private readonly ILogger<RefundOrderHandler> _logger;

    public RefundOrderHandler(
        IOrderRepository orderRepository,
        IPaymentTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        INombaClient nombaClient,
        ILogger<RefundOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _nombaClient = nombaClient;
        _logger = logger;
    }

    public async Task<Result<RefundOrderResult>> HandleAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<RefundOrderResult>.Failure("Order not found.", "ORDER_NOT_FOUND");

        if (order.Status is not (OrderStatus.Paid or OrderStatus.PartiallyRefunded))
            return Result<RefundOrderResult>.Failure($"Cannot refund an order in status {order.Status}.", "INVALID_ORDER_STATE");

        if (string.IsNullOrWhiteSpace(order.NombaTransactionId))
            return Result<RefundOrderResult>.Failure("Order has no associated Nomba transaction to refund.", "MISSING_TRANSACTION");

        var isFullRefund = request.Amount is null || request.Amount >= order.TotalAmount;

        RefundResult refundResult;
        try
        {
            refundResult = await _nombaClient.RefundTransactionAsync(new RefundRequest
            {
                TransactionId = order.NombaTransactionId,
                Amount = request.Amount,
                Reason = request.Reason
            }, ct);
        }
        catch (NombaApiException ex)
        {
            _logger.LogError(ex, "Refund failed for order {OrderReference}", order.OrderReference);
            return Result<RefundOrderResult>.Failure($"Refund failed: {ex.Message}", ex.NombaCode ?? "NOMBA_ERROR");
        }

        if (!refundResult.Success)
            return Result<RefundOrderResult>.Failure(refundResult.Message, "REFUND_DECLINED");

        var refundedAmount = request.Amount ?? order.TotalAmount;
        var record = PaymentTransaction.ForRefund(
            order.Id,
            order.NombaTransactionId,
            refundedAmount,
            isFullRefund ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded);

        await _transactionRepository.AddAsync(record, ct);

        order.MarkRefunded(isFullRefund);
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RefundOrderResult>.Success(new RefundOrderResult
        {
            OrderId = order.Id,
            IsFullRefund = isFullRefund,
            Message = refundResult.Message
        });
    }
}
