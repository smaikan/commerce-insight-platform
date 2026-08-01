# Project Admin Contract Guardrails

Verify exact fields and operations in `docs/api/api-project-docs/openapi-controller-contract.json` and the relevant Markdown contract before implementation.

## Authorization and errors

- Admin screens require backend Admin authorization; hidden navigation is not security.
- Distinguish `401` session loss from `403` insufficient permission.
- Map ProblemDetails validation errors to fields and retain `code`/safe `traceId` for recovery/support.
- On `409`, re-read current data; never blindly retry or overwrite.
- Use numeric enum wire values exactly as documented.

## Catalog

- Product writes, bulk create, status, activation, featured, relations, variants, price, stock movement, and images are separate documented operations.
- Products use `P...` public IDs; variants/images use GUIDs.
- Product list supports documented search/type/brand/status/active/featured/sort filters.
- Brand, collection, product type, and tag management support their documented list/create/bulk/update/activation behavior.
- Reviews can be approved and product metrics read only through documented engagement operations.

## Orders, returns, and customers

- Admin order list/detail/status operations are documented; e-commerce `Order` is distinct from `AccountingSalesOrder`.
- Generic customer-side order status mutation does not exist.
- Return lifecycle is Pending → Approved/Rejected → Received → Completed; render only valid actions.
- Admin user list/detail supports documented search/role/status filters and role/status changes; last-active-admin rules can return conflict.

## Inventory and configuration

- StockMovement is the physical stock ledger. Product variant stock is not directly editable.
- Signed single movement and atomic bulk movement are supported; bulk maximum is 500 rows.
- Workflow-owned stock types cannot be selected for manual movement.
- Shipping methods and tax rates have documented admin list/detail/create/update/activation operations.
- Coupons have documented list/create/update/activation behavior; do not invent campaign types or performance metrics.

## Accounting

- Every accounting endpoint is Admin-only.
- Accounting sales orders are independent from e-commerce cart/orders.
- Draft documents are editable; posting/cancellation follows documented effects and idempotency.
- Reversed/cancelled history remains visible.
- Balances, totals, VAT, paid/remaining, FIFO cost, and profit are authoritative API values.
- Reports have report-specific meaning and no universal frontend grand-total contract.

## Dashboard truthfulness

Before showing a metric, identify its endpoint, filters, period, scope, and failure state. A paged response's `totalCount` may support a scoped count; current-page sums do not support global money totals. Use accounting report data only with its documented column semantics. If no authoritative source exists, omit the metric or mark the proposed backend contract as required.
