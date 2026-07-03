using Microsoft.AspNetCore.Mvc;
using NombaCommerceConnect.Application.Payments;
using NombaCommerceConnect.Infrastructure.Payments.Nomba;
using Microsoft.Extensions.Options;

namespace NombaCommerceConnect.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly HandleNombaWebhookHandler _handler;
    private readonly NombaOptions _options;

    public WebhooksController(HandleNombaWebhookHandler handler, IOptions<NombaOptions> options)
    {
        _handler = handler;
        _options = options.Value;
    }

    /// <summary>
    /// Receives Nomba payment event notifications (e.g. payment_success). The raw
    /// body is read manually (rather than bound as a typed model) because signature
    /// verification must run against the exact bytes Nomba sent, before any
    /// deserialization happens.
    /// </summary>
    [HttpPost("nomba")]
    public async Task<IActionResult> ReceiveNombaWebhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signature = Request.Headers.TryGetValue(_options.WebhookSignatureHeaderName, out var values)
            ? values.ToString()
            : null;

        var result = await _handler.HandleAsync(rawBody, signature, ct);

        if (!result.IsSuccess)
        {
            // 401 for a bad signature so it's visibly distinct from a processing error;
            // everything else still returns 200 to prevent Nomba retrying a payload we
            // understood but couldn't act on (e.g. an unknown order reference).
            if (result.ErrorCode == "INVALID_SIGNATURE")
                return Unauthorized(new { error = result.Error });

            return Ok(new { received = true, warning = result.Error });
        }

        return Ok(new { received = true });
    }
}
