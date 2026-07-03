using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NombaCommerceConnect.Application.Nomba;
using NombaCommerceConnect.Application.Nomba.Models;

namespace NombaCommerceConnect.Infrastructure.Payments.Nomba;

/// <summary>
/// Real HTTP-backed implementation of <see cref="INombaClient"/>. Every call attaches
/// a fresh bearer token (via <see cref="INombaAuthTokenProvider"/>) and the merchant's
/// accountId header, per Nomba's documented authentication scheme.
/// </summary>
public class NombaClient : INombaClient
{
    private readonly HttpClient _httpClient;
    private readonly INombaAuthTokenProvider _tokenProvider;
    private readonly NombaOptions _options;
    private readonly ILogger<NombaClient> _logger;

    public NombaClient(
        HttpClient httpClient,
        INombaAuthTokenProvider tokenProvider,
        IOptions<NombaOptions> options,
        ILogger<NombaClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CheckoutOrderResult> CreateCheckoutOrderAsync(CreateCheckoutOrderRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            order = new
            {
                orderReference = request.OrderReference,
                amount = request.Amount.ToString("F2"),
                currency = request.Currency,
                customerEmail = request.CustomerEmail,
                customerId = request.CustomerId,
                callbackUrl = request.CallbackUrl,
                splitRequest = request.SplitRequest is null ? null : new
                {
                    splitType = request.SplitRequest.SplitType,
                    splitList = request.SplitRequest.SplitList.Select(s => new
                    {
                        accountId = s.AccountId,
                        value = s.Value
                    })
                }
            }
        };

        var envelope = await SendAsync<CheckoutOrderData>(HttpMethod.Post, "/v1/checkout/order", body, ct);

        return new CheckoutOrderResult
        {
            CheckoutLink = envelope.CheckoutLink,
            OrderReference = envelope.OrderReference
        };
    }

    public async Task<TransactionStatusResult> VerifyTransactionAsync(string orderReference, CancellationToken ct = default)
    {
        var path = $"{_options.VerifyTransactionEndpointPath}?orderReference={Uri.EscapeDataString(orderReference)}";
        var envelope = await SendAsync<TransactionStatusData>(HttpMethod.Get, path, null, ct);

        return new TransactionStatusResult
        {
            TransactionId = envelope.TransactionId,
            OrderReference = orderReference,
            Status = envelope.Status,
            Amount = envelope.Amount
        };
    }

    public async Task<RefundResult> RefundTransactionAsync(RefundRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            transactionId = request.TransactionId,
            amount = request.Amount?.ToString("F2"),
            reason = request.Reason
        };

        var envelope = await SendAsync<RefundData>(HttpMethod.Post, _options.RefundEndpointPath, body, ct);

        return new RefundResult
        {
            Success = envelope.Status,
            Message = envelope.Message,
            RefundReference = envelope.RefundReference
        };
    }

    private async Task<TData> SendAsync<TData>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct);

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("accountId", _options.AccountId);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        NombaEnvelope<TData>? envelope;
        try
        {
            envelope = System.Text.Json.JsonSerializer.Deserialize<NombaEnvelope<TData>>(responseBody, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Nomba response body from {Path}: {Body}", path, responseBody);
            throw new NombaApiException("Nomba returned an unparseable response.", httpStatusCode: (int)response.StatusCode, innerException: ex);
        }

        if (!response.IsSuccessStatusCode || envelope is null || !string.Equals(envelope.Code, "00", StringComparison.Ordinal))
        {
            var description = envelope?.Description ?? responseBody;
            _logger.LogWarning("Nomba API error on {Path}: {Code} - {Description}", path, envelope?.Code, description);
            throw new NombaApiException(description, envelope?.Code, (int)response.StatusCode);
        }

        if (envelope.Data is null)
            throw new NombaApiException($"Nomba response for {path} did not include a data payload.");

        return envelope.Data;
    }

    private class NombaEnvelope<TData>
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;

        [JsonPropertyName("description")]
        public string Description { get; set; } = default!;

        [JsonPropertyName("data")]
        public TData? Data { get; set; }
    }

    private class CheckoutOrderData
    {
        [JsonPropertyName("checkoutLink")]
        public string CheckoutLink { get; set; } = default!;

        [JsonPropertyName("orderReference")]
        public string OrderReference { get; set; } = default!;
    }

    private class TransactionStatusData
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; } = default!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    private class RefundData
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("refundReference")]
        public string? RefundReference { get; set; }
    }
}
