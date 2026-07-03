using Microsoft.AspNetCore.Mvc;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Application.Orders;
using NombaCommerceConnect.Application.Payments;

namespace NombaCommerceConnect.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly PlaceOrderHandler _placeOrderHandler;
    private readonly RefundOrderHandler _refundOrderHandler;
    private readonly IOrderRepository _orderRepository;

    public OrdersController(
        PlaceOrderHandler placeOrderHandler,
        RefundOrderHandler refundOrderHandler,
        IOrderRepository orderRepository)
    {
        _placeOrderHandler = placeOrderHandler;
        _refundOrderHandler = refundOrderHandler;
        _orderRepository = orderRepository;
    }

    public record CheckoutItem(Guid ProductId, int Quantity);
    public record CheckoutRequest(string CustomerEmail, string CustomerFullName, string CallbackUrl, List<CheckoutItem> Items);
    public record RefundRequestBody(decimal? Amount, string? Reason);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(id, ct);
        if (order is null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.OrderReference,
            Status = order.Status.ToString(),
            order.TotalAmount,
            order.Currency,
            order.CheckoutLink,
            order.NombaTransactionId,
            order.CreatedAtUtc,
            order.PaidAtUtc,
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.VendorId,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal
            })
        });
    }

    /// <summary>
    /// Places an order for the given cart and returns a Nomba checkout link for the
    /// customer to complete payment. This is the "Checkout API" integration point.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var result = await _placeOrderHandler.HandleAsync(new PlaceOrderRequest
        {
            CustomerEmail = request.CustomerEmail,
            CustomerFullName = request.CustomerFullName,
            CallbackUrl = request.CallbackUrl,
            Items = request.Items.Select(i => new PlaceOrderItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
        }, ct);

        if (!result.IsSuccess)
            return MapFailure(result.ErrorCode, result.Error!);

        return Ok(result.Value);
    }

    /// <summary>
    /// Issues a refund against a paid order. This is the "Refunds API" integration point.
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequestBody body, CancellationToken ct)
    {
        var result = await _refundOrderHandler.HandleAsync(new RefundOrderRequest
        {
            OrderId = id,
            Amount = body.Amount,
            Reason = body.Reason
        }, ct);

        if (!result.IsSuccess)
            return MapFailure(result.ErrorCode, result.Error!);

        return Ok(result.Value);
    }

    private IActionResult MapFailure(string? errorCode, string error)
    {
        var payload = new { error, code = errorCode };

        return errorCode switch
        {
            "ORDER_NOT_FOUND" or "PRODUCT_NOT_FOUND" => NotFound(payload),
            "NOMBA_ERROR" => StatusCode(StatusCodes.Status502BadGateway, payload),
            _ => BadRequest(payload)
        };
    }
}
