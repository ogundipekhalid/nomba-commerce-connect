using Microsoft.Extensions.Logging;
using NombaCommerceConnect.Application.Common;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Application.Nomba.Models;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Application.Orders;

/// <summary>
/// Turns a cart (product ids + quantities) into a persisted Order and a Nomba checkout
/// link, splitting the payment across each product's vendor by their share of the total.
/// </summary>
public class PlaceOrderHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INombaClient _nombaClient;
    private readonly ILogger<PlaceOrderHandler> _logger;

    public PlaceOrderHandler(
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        INombaClient nombaClient,
        ILogger<PlaceOrderHandler> logger)
    {
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _nombaClient = nombaClient;
        _logger = logger;
    }

    public async Task<Result<PlaceOrderResult>> HandleAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
            return Result<PlaceOrderResult>.Failure("An order must contain at least one item.", "EMPTY_CART");

        // 1. Resolve or create the customer.
        var customer = await _customerRepository.GetByEmailAsync(request.CustomerEmail, ct);
        if (customer is null)
        {
            customer = new Customer(request.CustomerFullName, request.CustomerEmail);
            await _customerRepository.AddAsync(customer, ct);
        }

        // 2. Load products with their vendor loaded (needed to snapshot NombaAccountId).
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsWithVendorAsync(productIds, ct);
        var productLookup = products.ToDictionary(p => p.Id);

        var missing = productIds.Where(id => !productLookup.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return Result<PlaceOrderResult>.Failure($"Unknown product id(s): {string.Join(", ", missing)}.", "PRODUCT_NOT_FOUND");

        // 3. Validate stock and build order line items, reducing stock as we go.
        var orderItems = new List<OrderItem>();
        foreach (var line in request.Items)
        {
            var product = productLookup[line.ProductId];

            if (!product.IsActive)
                return Result<PlaceOrderResult>.Failure($"Product '{product.Name}' is no longer available.", "PRODUCT_INACTIVE");

            if (line.Quantity <= 0)
                return Result<PlaceOrderResult>.Failure("Quantity must be greater than zero.", "INVALID_QUANTITY");

            if (product.StockQuantity < line.Quantity)
                return Result<PlaceOrderResult>.Failure(
                    $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}.", "INSUFFICIENT_STOCK");

            product.ReduceStock(line.Quantity);
            orderItems.Add(new OrderItem(product, line.Quantity));
        }

        // 4. Create and persist the order (status: Created).
        var order = Order.Create(customer.Id, orderItems);
        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 5. Build the Nomba checkout request, including a split across vendors when
        //    the cart spans more than one vendor.
        var vendorSplits = order.GetVendorSplitPercentages();
        SplitRequestDto? splitRequest = null;
        if (vendorSplits.Count > 1)
        {
            splitRequest = new SplitRequestDto
            {
                SplitType = "PERCENTAGE",
                SplitList = vendorSplits
                    .Select(kv => new SplitEntryDto { AccountId = kv.Key, Value = kv.Value })
                    .ToList()
            };
        }

        var checkoutRequest = new CreateCheckoutOrderRequest
        {
            OrderReference = order.OrderReference,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            CustomerEmail = customer.Email,
            CustomerId = customer.Id.ToString(),
            CallbackUrl = request.CallbackUrl,
            SplitRequest = splitRequest
        };

        // 6. Call Nomba. Any API failure here is a recoverable/expected error state -
        //    the order stays in `Created` and the customer can retry checkout.
        CheckoutOrderResult checkoutResult;
        try
        {
            checkoutResult = await _nombaClient.CreateCheckoutOrderAsync(checkoutRequest, ct);
        }
        catch (NombaApiException ex)
        {
            _logger.LogWarning(ex, "Nomba checkout order creation failed for order {OrderReference}", order.OrderReference);
            return Result<PlaceOrderResult>.Failure($"Payment provider error: {ex.Message}", ex.NombaCode ?? "NOMBA_ERROR");
        }

        // 7. Move the order to PendingPayment and persist the checkout link.
        order.MarkPendingPayment(checkoutResult.CheckoutLink);
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PlaceOrderResult>.Success(new PlaceOrderResult
        {
            OrderId = order.Id,
            OrderReference = order.OrderReference,
            CheckoutLink = checkoutResult.CheckoutLink,
            TotalAmount = order.TotalAmount
        });
    }
}
