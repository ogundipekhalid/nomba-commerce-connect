using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NombaCommerceConnect.Application.Nomba;

namespace NombaCommerceConnect.Infrastructure.Payments.Nomba;

public interface INombaAuthTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}

/// <summary>
/// Exchanges Nomba client credentials for a bearer token via
/// <c>POST /v1/auth/token/issue</c> and caches it in memory, refreshing shortly
/// before the documented 30-minute expiry. A semaphore prevents multiple concurrent
/// requests from all triggering a refresh at once.
/// </summary>
public class NombaAuthTokenProvider : INombaAuthTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly NombaOptions _options;
    private readonly ILogger<NombaAuthTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    public NombaAuthTokenProvider(HttpClient httpClient, IOptions<NombaOptions> options, ILogger<NombaAuthTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc - RefreshSkew)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Re-check in case another caller already refreshed while we waited.
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc - RefreshSkew)
                return _cachedToken;

            return await IssueNewTokenAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> IssueNewTokenAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/token/issue");
        request.Headers.Add("accountId", _options.AccountId);
        request.Content = JsonContent.Create(new
        {
            grant_type = "client_credentials",
            client_id = _options.ClientId,
            client_secret = _options.ClientSecret
        });

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Nomba token issue failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new NombaApiException(
                "Failed to obtain a Nomba access token.",
                httpStatusCode: (int)response.StatusCode);
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
            ?? throw new NombaApiException("Nomba token response was empty or malformed.");

        _cachedToken = tokenResponse.AccessToken;
        _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        return _cachedToken;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 1800;
    }
}
