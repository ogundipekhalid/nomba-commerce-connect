<<<<<<< HEAD
# Nomba Commerce Connect

A layered .NET connector service that wraps Nomba's **Checkout**, **Webhooks**,
**Refunds**, and **split-payment** APIs behind a clean, documented REST surface —
demonstrated end-to-end via a reference marketplace storefront (vendors list
products, customers check out, payment is split across vendors automatically).

Built for the DevCareer × Nomba Hackathon — Build Phase progress submission.

---

## Status at a glance (Build Phase progress check)

| Area | Status |
|---|---|
| Layered architecture (Domain / Application / Infrastructure / Api) | ✅ Done |
| Domain model + order/split business rules | ✅ Done, unit tested |
| `INombaClient` abstraction (Checkout, Verify, Refund) | ✅ Coded |
| Nomba OAuth2 token provider (issue + auto-refresh) | ✅ Coded |
| Webhook signature verification | ✅ Coded, unit tested |
| Webhook idempotency + server-side re-verification | ✅ Coded |
| Refund flow | ✅ Coded |
| Reference storefront (plain HTML/JS/fetch) | ✅ Done |
| `FakeNombaClient` (mock, used until credentials arrive) | ✅ Done — **currently active by default** |
| Live sandbox transaction against real Nomba API | ⏳ **Blocked on Nomba API credentials**, requested via #nomba-hackathon |
| Docker packaging | ✅ Done |

**Why a fake client is wired in by default:** Nomba API credentials had not been
issued at the time of this submission (requested in `#nomba-hackathon` on the
DevCareer Slack). Rather than leave the payment integration unbuilt, the entire
system — order flow, split calculation, webhook handling, refunds — is built
against the `INombaClient` interface and runs fully end-to-end with
`FakeNombaClient`, which returns responses shaped exactly like Nomba's documented
API. Swapping to the real client once credentials arrive is a one-line config
change (see [Switching to the real Nomba API](#switching-to-the-real-nomba-api)).

---

## Architecture

```
NombaCommerceConnect.sln
├── src/
│   ├── NombaCommerceConnect.Domain          # Entities + business rules, zero dependencies
│   │   Vendor, Customer, Product, Order, OrderItem, PaymentTransaction
│   │
│   ├── NombaCommerceConnect.Application     # Use cases + interfaces (no infra dependencies)
│   │   Orders/PlaceOrderHandler             # cart -> order -> Nomba checkout order (with split)
│   │   Payments/HandleNombaWebhookHandler   # verify signature -> re-verify txn -> mark paid
│   │   Payments/RefundOrderHandler          # refund a paid order via Nomba
│   │   Nomba/INombaClient                   # abstraction implemented twice (real + fake)
│   │
│   ├── NombaCommerceConnect.Infrastructure  # EF Core (SQLite) + real Nomba HTTP client
│   │   Payments/Nomba/NombaAuthTokenProvider   # OAuth2 client-credentials, caches + refreshes
│   │   Payments/Nomba/NombaClient              # real HTTP-backed INombaClient
│   │   Payments/Nomba/FakeNombaClient          # mock INombaClient (active by default)
│   │   Payments/Nomba/NombaWebhookSignatureVerifier
│   │
│   └── NombaCommerceConnect.Api             # ASP.NET Core Web API + static-file storefront
│       Controllers/ (Vendors, Products, Orders, Webhooks)
│       wwwroot/ (index.html, app.js, styles.css) — plain JS/fetch reference client
│
└── tests/NombaCommerceConnect.Tests
    OrderSplitCalculationTests               # vendor split-percentage math
    NombaWebhookSignatureVerifierTests        # HMAC signature verification
```

**Dependency direction:** `Api -> Infrastructure -> Application -> Domain`, and
`Api -> Application` for calling handlers directly. Domain has no dependencies at
all; Application depends only on Domain and defines interfaces that Infrastructure
implements — standard Clean/Onion layering, so the Nomba integration (or the
database) can be swapped without touching business logic.

---

## Order flow (Checkout API + split payments)

1. Customer adds products (possibly from multiple vendors) to a cart in the
   storefront and submits checkout.
2. `POST /api/orders` → `PlaceOrderHandler`:
   - resolves/creates the customer, validates stock, reduces it, creates the `Order` aggregate.
   - computes each vendor's percentage share of the order total (`Order.GetVendorSplitPercentages()`).
   - calls `INombaClient.CreateCheckoutOrderAsync`, passing a `splitRequest` when more than one vendor is involved.
   - returns the Nomba `checkoutLink` to the customer.
3. Customer completes payment on Nomba's hosted checkout page.

## Payment confirmation (Webhooks)

`POST /api/webhooks/nomba` → `HandleNombaWebhookHandler`, following Nomba's own
guidance rather than trusting a webhook body at face value:

1. **Verify the signature** (HMAC-SHA256 over the raw body, constant-time compare) before parsing anything.
2. **Check idempotency** — Nomba's `requestId` is stored per processed event; duplicate deliveries are acknowledged and skipped rather than double-processed.
3. **Re-verify server-side** — the transaction is looked up again via the Nomba API (not just trusted from the webhook payload) before the order is marked `Paid`.

## Refunds

`POST /api/orders/{id}/refund` → `RefundOrderHandler` calls Nomba's refund endpoint
against the order's stored transaction id, supports full or partial amounts, and
records the refund as a `PaymentTransaction` for audit history.

---

## Assumptions & open questions (being transparent for the judges)

- **Vendor Nomba sub-accounts are not provisioned by this system.** Split payments
  require each vendor to have their own Nomba `accountId`. Sub-account creation
  appears to be dashboard-driven rather than a public self-serve API, so this build
  seeds two demo vendors with placeholder account ids (`SEED-ACCOUNT-1/2`) to
  demonstrate the split mechanism. Real vendor onboarding is out of scope for this
  progress stage.
- **The exact refund endpoint path is unconfirmed.** Nomba's docs nav lists a
  "Refund checkout transaction" endpoint under Online Checkout, but the full path
  wasn't visible without deeper API reference/console access. `NombaClient` calls a
  best-guess path (`/v1/checkout/transaction/refund`), configurable via
  `Nomba:RefundEndpointPath` in `appsettings.json` — a one-line fix once confirmed.
- **The webhook signature header name/scheme is unconfirmed.** Implemented as
  HMAC-SHA256 over the raw payload per the general pattern Nomba documents; the
  exact header name (`Nomba:WebhookSignatureHeaderName`, defaulted to
  `nomba-sig-value`) should be confirmed against the dashboard once a webhook is
  registered.

---

## Running locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone <this-repo>
cd NombaCommerceConnect
dotnet restore
dotnet run --project src/NombaCommerceConnect.Api
```

The app auto-creates a local SQLite database and seeds two demo vendors + four
products on first run (Development environment only). Open
**http://localhost:5080** for the storefront, or **http://localhost:5080/swagger**
for the API reference.

### Running the tests

```bash
dotnet test
```

### Running via Docker

```bash
docker compose up --build
```

Then visit **http://localhost:8080**.

---

## Switching to the real Nomba API

Once credentials are issued:

1. Set in `appsettings.json` / environment variables / `dotnet user-secrets`:
   ```json
   {
     "Nomba": {
       "UseFakeClient": false,
       "BaseUrl": "https://sandbox.nomba.com",
       "ClientId": "<from dashboard>",
       "ClientSecret": "<from dashboard>",
       "AccountId": "<from dashboard>",
       "WebhookSigningKey": "<from dashboard>"
     }
   }
   ```
2. Register your webhook URL (`https://<your-host>/api/webhooks/nomba`) on the
   Nomba dashboard and subscribe to `payment_success`.
3. Confirm `Nomba:RefundEndpointPath` and `Nomba:WebhookSignatureHeaderName`
   against the live docs/dashboard (see Assumptions above) and adjust if needed.

No other code changes are required — `DependencyInjection.AddInfrastructure`
switches between `FakeNombaClient` and the real `NombaClient` purely based on
`Nomba:UseFakeClient`.

---

## API reference (summary)

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/vendors` | List vendors |
| `POST` | `/api/vendors` | Create a vendor (`businessName`, `email`, `nombaAccountId`) |
| `GET` | `/api/products` | List active products |
| `POST` | `/api/products` | Create a product (`vendorId`, `name`, `description`, `price`, `stockQuantity`, `imageUrl`) |
| `POST` | `/api/orders` | Checkout — creates an order + Nomba checkout link |
| `GET` | `/api/orders/{id}` | Fetch order status/details |
| `POST` | `/api/orders/{id}/refund` | Refund a paid order (optional `amount`, `reason`) |
| `POST` | `/api/webhooks/nomba` | Nomba webhook receiver |

Full interactive reference at `/swagger` when running in Development.

### Example: place an order

```bash
curl -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerEmail": "buyer@example.com",
    "customerFullName": "Ada Lovelace",
    "callbackUrl": "http://localhost:5080/index.html",
    "items": [ { "productId": "<product-guid>", "quantity": 1 } ]
  }'
```

### Example: refund an order

```bash
curl -X POST http://localhost:5080/api/orders/<order-guid>/refund \
  -H "Content-Type: application/json" \
  -d '{ "reason": "Customer changed their mind" }'
```

---

## Tech stack

C# / .NET 8 · ASP.NET Core Web API · EF Core (SQLite) · xUnit · plain HTML/CSS/JS
(no framework) for the storefront · Docker.
=======
# nomba-commerce-connect
Layered .NET connector for Nomba's Checkout, Webhooks, Refunds and split-payment APIs, with a reference storefront — built for the DevCareer x Nomba Hackathon.
Notes from building this
Started this after getting into the Build Phase — spent most of the time getting the Nomba API flow (auth, checkout, webhooks) right since I hadn't worked with their split payment endpoint before. Still waiting on live API credentials from Nomba (asked in the Slack channel), so right now the payment calls are mocked but built against their real documented request/response shapes — swapping to live is one config flag once I get access. Next up: wrapping this as an actual OpenCart plugin so it matches a real e-commerce platform instead of just my own test storefront.
>>>>>>> 6e85ba9d0a1e67365bd5a9ff99b6c78afd323d07
