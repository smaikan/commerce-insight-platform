# Project Commerce Test Flows

Use these expectations with the current contracts under `docs/api/api-project-docs`. Confirm routes and UI labels from the implemented frontend before writing locators.

## Contract facts

- Public product IDs use the `P...` format; product-variant IDs are GUIDs.
- Public product reads use `GET /api/products` and `GET /api/products/{productPublicId}`.
- Anonymous carts use the backend-managed `ecommerce_guest_cart` Secure/HttpOnly/SameSite=Lax cookie.
- Every mutation of an existing cart requires the latest `concurrencyToken`.
- Cart request bodies must not provide authoritative product IDs, prices, stock, or totals.
- Checkout uses `POST /api/orders`, requires an authenticated user, and consumes the current cart.
- Checkout accepts `expectedCartConcurrencyToken`, optional address, coupon, and shipping-method IDs.
- Payment creation uses `POST /api/orders/{id}/payments` with a required stable `Idempotency-Key`.
- Cart conflicts return `409` and must not be retried blindly.
- Cart, order, and payment endpoints have different rate limits.
- ASP.NET errors use ProblemDetails with fields such as `status`, `code`, `errors`, and `traceId`.

## Login and session matrix

Cover:

- valid login sets a server-managed session and reaches the intended same-origin destination;
- invalid credentials show a safe non-enumerating message and retain the email but not the password;
- empty/invalid fields expose associated inline errors and move focus predictably;
- direct protected-route navigation redirects to login without an unsafe external `returnTo`;
- expired access with valid refresh rotates the session once and completes the original read;
- failed refresh clears the local session and returns to login without a redirect loop;
- logout revokes upstream refresh state when possible, clears local cookies, and blocks protected content;
- browser history after logout does not reveal usable private content;
- guest cart merges once after login and the guest-cart cookie is removed;
- `401`, `403`, `429`, network failure, and unexpected server failure produce distinct recoverable UI.

Assert that tokens never appear in DOM, URLs, client storage, console output, screenshots, or frontend JSON payloads.

## Product matrix

Cover:

- list loading, populated, empty, pagination, search/filter, and API failure states;
- product detail with title, price, image, description, variants, stock/availability, and canonical navigation;
- variant selection updates the exact displayed state and add-to-cart request;
- inactive, missing, unavailable, and out-of-stock products disable or replace invalid purchase actions;
- main and gallery images have meaningful alternatives; decorative imagery uses empty alternatives;
- keyboard and screen-reader users can identify the selected variant, price, availability, quantity, and add result;
- add-to-cart reports success without stealing focus unexpectedly and exposes the updated cart state.

Do not assert a frontend-calculated net price, tax, popularity, or stock balance.

## Cart matrix

Cover:

- first anonymous add creates the cart/cookie and renders authoritative response values;
- repeated add, quantity increase/decrease, item removal, and cart clear use the latest token;
- page refresh and a second page preserve the expected guest or authenticated cart;
- empty-cart and recoverable API-error states expose an appropriate next action;
- stale token produces `409`, refreshes the current cart, explains the conflict, and does not silently overwrite;
- `isAvailable=false` prevents checkout and identifies the affected item;
- `priceChanged=true` exposes the old/current state and requires acknowledgement according to the UI design;
- login merges the guest cart once and retains correct quantities per backend result;
- rapid repeated activation does not create unintended duplicate mutations;
- totals, quantity, availability, and prices match the last authoritative cart response.

Include a multi-tab concurrency case when the implementation permits it.

## Checkout and payment matrix

Use only a local/test environment and a sandbox or explicitly safe fake provider.

Cover:

- anonymous checkout login istemeden zorunlu müşteri, shipping adresi, aktif kargo, cart concurrency ve idempotency ile tamamlanır;
- guest checkout'ta member-only kupon, challenge/rate-limit/fallback, sıfır toplamlı sipariş ve pasif kargo durumları test edilir;
- guest magic-link aynı/farklı cihaz, tek kullanım/süre sonu/resend, session/CSRF/origin ve çapraz-order 404 akışları test edilir;
- guest ödeme/iptal/iade/değişim ile aynı e-postalı güvenli claim ve claim öncesi review/rating engeli test edilir;
- current cart is re-read before submission;
- unavailable items, price changes, stale concurrency, empty cart, and insufficient stock prevent or recover checkout safely;
- required address fields, shipping choice, coupon state, consent, and payment controls have accessible validation;
- address ownership is enforced; another user's address cannot be selected by manipulating requests;
- shipping fee, coupon, tax, totals, order number, and reservation state come from the API result;
- submit becomes visibly busy and resistant to accidental double activation;
- retrying the same checkout/payment intent preserves its idempotency key;
- a new user intent receives a new idempotency key;
- payment pending, paid, failed, cancelled, network timeout, and unknown-result states have honest messaging;
- refresh/back navigation does not create a second order or payment;
- successful checkout reaches an accessible confirmation containing the authoritative order identifier;
- `400`, `401`, `403`, `409`, `429`, and `500` paths preserve safe input and expose the correct recovery action.

Never test production payment credentials or log full addresses, tokens, provider payloads, or personal data.
