# Accounting Frontend Rules

Read this file for any accounting route, form, table, action, or report.

## Domain boundaries

- Keep `AccountingSalesOrder` independent from e-commerce `Order` and `Cart`.
- Treat `SalesInvoice` as an optional document linked to the accounting sale, not a second stock/receivable event.
- Allocate payments to `CurrentAccountTransaction`, never directly to a sales invoice.
- Treat StockMovement as the only physical stock ledger and FIFO layers as the accounting cost ledger.

## Lifecycles

- Sales orders, sales invoices, and purchase invoices use Draft, Posted, and Cancelled.
- Allow header/line editing only in Draft.
- Posting is idempotent and may create stock, FIFO, or current-account effects according to the document type.
- Cancellation/reversal preserves the original record and creates or exposes reversing effects.
- Re-read details after posting, cancellation, payment cancellation, or financial reversal.

## Payments and treasury

- Require exactly one cash account or bank account.
- CustomerCollection requires at least one receivable allocation.
- SupplierPayment may have an empty allocation and remain an unallocated supplier advance.
- Allocations cannot exceed the payment or the target transaction's remaining amount.
- Balances are derived; never submit or locally mutate them.
- Preserve the same idempotency key for a retry of the same create intent.

## Purchases and costs

- A PurchaseInvoice does not create a physical StockMovement.
- Fully allocate every purchase line to eligible positive Purchase movements before posting.
- Purchase-related expenses may allocate by VAT-exclusive line amount, quantity, or explicit manual amounts.
- General expenses do not alter inventory cost.
- Update only the remaining opening-balance cost with the latest concurrency token.
- On concurrency conflict, refresh and ask for confirmation; never silently reapply.

## Currency, amounts, and reports

- Current accounting behavior is TRY with exchange rate `1`.
- Display money consistently, but trust API-calculated VAT, discount, totals, cost, margin, balance, paid, and remaining fields.
- Reports share a generic row shape whose monetary columns mean different things per report.
- Define a column map per report; never render one universal finance table.
- Do not present the current page's rows as a grand total because the API provides no report-total contract.
- Reset `pageNumber` to `1` when report filters change.

## UI response rules

- Render validation errors at their fields.
- Preserve drafts on `business_rule_violation`, `conflict`, and `concurrency_conflict`.
- Show cancelled/reversed rows and lifecycle metadata rather than deleting them.
- Disable unavailable actions based on current status, but still enforce authorization server-side.
- Use the receivables/debts reports for payment selection with the understanding that they are not locked allocation queries.
