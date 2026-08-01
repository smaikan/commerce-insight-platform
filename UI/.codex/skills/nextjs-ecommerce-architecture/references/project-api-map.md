# Project API Map

Use this map for feature discovery. Verify exact request and response fields against `docs/api/api-project-docs/openapi-controller-contract.json` and the relevant Markdown contract.

## Global contracts

- Backend exposes 206 operations across 33 controllers; the checked OpenAPI document contains 159 paths and 208 schemas.
- Public IDs: users use `U...`, products use `P...`; orders, variants, and accounting records mostly use UUIDs.
- Enums are numeric on the wire.
- Lists commonly use `PagedResult<T>`.
- Errors use ProblemDetails with `code`, `traceId`, and optional validation `errors`.
- Idempotency and concurrency are endpoint-specific; never apply blind retries.

## Public and customer features

- Auth: register, login, refresh, logout, forgot/reset password.
- Customer: profile, email/password changes, account closure, session management, addresses.
- Catalog: products, variants, images, brands, collections, tags, product types.
- Engagement: favorites, ratings, reviews, activity, and admin metrics.
- Cart: guest HttpOnly cookie cart, authenticated cart, merge, optimistic concurrency.
- Checkout: cart-based order creation, shipping, coupon, reservation, provider payment.
- Orders: customer list/detail/cancel and admin lifecycle management.
- Returns: refund or exchange request; admin approve, reject, receive, and complete.

## General administration

- Product, variant, relation, image, status, activation, and featured management.
- Signed stock movements, atomic bulk movement, and balance comparison.
- Users and roles.
- Shipping methods, tax rates, and coupons.
- Orders, returns, review approval, and product metrics.

## Accounting features

All accounting endpoints require Admin authorization.

- Current accounts: customer, supplier, or both.
- Accounting sales orders independent from e-commerce orders/carts.
- Optional sales invoices that do not create a second stock or receivable effect.
- Purchase invoices allocated to existing positive purchase stock movements.
- General expenses and purchase-invoice expenses.
- Customer collections, supplier payments, and unallocated supplier advances.
- Cash accounts, bank accounts, statements, manual financial transactions, reversals, and atomic bank transfers.
- FIFO cost layers, remaining opening-cost updates, and variant cost history.
- Twenty-eight read-only reports covering sales, invoices, stock costing, valuation, profitability, current accounts, receivables/debts, overdue balances, payments, cash/bank movements, and VAT.

## Explicitly unavailable accounting features

Do not design working UI or API integrations for:

- Current-account, cash, or bank opening balances.
- Sales/purchase return invoices and partial-return accounting automation.
- Debit/credit notes, exchange-rate differences, or price-difference notes.
- General-expense update, post, cancel, or delete.
- Attachments and document archive.
- Financial periods, locking, and closing.
- Report export/print contracts.
- Bank reconciliation/import, cheques, and promissory notes.
- Non-admin accounting roles or approval matrices.
- E-invoice, ERP, bank, and marketplace accounting integrations.
