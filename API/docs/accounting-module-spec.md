I want you to add a comprehensive “Pre-Accounting Module” to this project. It must integrate with the existing e-commerce infrastructure while remaining as separate and independent from the core project as reasonably possible.

Respond to me in Turkish.

IMPORTANT WORKING RULE:

Do not write code during the first phase. First, analyze the entire project, identify the existing structures, and prepare an applicable implementation plan.

If you believe a change is required in the core project, explain why it is necessary and ask for my approval before applying it.

Without my explicit approval, do not:

- Modify the Product or ProductVariant entities.
- Modify the Order or OrderItem entities.
- Modify the StockMovement entity.
- Change the existing stock calculation logic.
- Change the existing order creation workflow.
- Introduce breaking changes to existing repository or interface contracts.
- Perform a large-scale refactor in core project folders.
- Modify or delete existing migrations.
- Rename or delete existing files.
- Commit or push changes.

When a requirement can be solved inside the Accounting module by using a mapping table, link table, adapter, or similar isolated structure, prefer that approach instead of changing the core project.

# 1. CURRENT PROJECT STATE

The project already contains:

- Products
- Product variants
- Orders
- Order items
- A stock management system

All stock movements are currently stored in the existing:

`StockMovement`

entity.

`StockMovement` is the project’s primary and only stock movement mechanism.

Do not create any of the following:

- StockTransaction
- InventoryTransaction
- An alternative StockMovement entity
- A second stock balance system
- An accounting-specific stock system

Do not implement a new mechanism that directly changes stock quantities on Product or ProductVariant.

Reuse the existing stock system exactly according to how it currently works.

The Accounting module must reuse the following existing project structures:

- Product
- ProductVariant
- StockMovement
- Existing stock services
- Existing repository infrastructure
- Existing UnitOfWork infrastructure
- Existing authorization and user infrastructure

Do not create duplicate versions of these entities inside the Accounting module.

Approved sales boundary:

- The existing e-commerce Order and OrderItem belong only to authenticated User/cart checkout.
- Accounting sales must not use or modify the existing Order, OrderItem, Cart, or e-commerce Address workflow.
- AccountingSalesOrder and AccountingSalesOrderItem are separate Accounting aggregates with a distinct CurrentAccount-based responsibility; they are not copies of the e-commerce checkout aggregate.
- The project currently has no Warehouse entity. This milestone uses one implicit warehouse, adds no WarehouseId to Accounting sales records, and does not introduce a Warehouse entity.

The Accounting module should connect to core entities primarily through their IDs.

To prevent historical invoice data from changing later, commercial snapshot fields such as the following may be stored on invoice lines at the time the invoice is posted:

- Product name
- SKU
- Barcode
- Variant description
- Customer title
- Supplier title

However, snapshot fields must not become duplicate master entities that replace Product, Customer, Supplier, or other existing core records.

# 2. MODULAR ARCHITECTURE

Keep the Accounting module as isolated from the core project as reasonably possible.

Follow the project’s existing:

- Clean Architecture structure
- CQRS structure
- MediatR structure
- Coding conventions
- Naming conventions

Adapt the following folder structure to the project’s actual names:

ECommerce.Domain
- Accounting
  - PurchaseInvoices
  - SalesOrders
  - SalesInvoices
  - CostLayers
  - CurrentAccounts
  - Payments
  - CashAndBank
  - Expenses
  - Common

ECommerce.Application
- Accounting
  - PurchaseInvoices
  - SalesOrders
  - SalesInvoices
  - CostLayers
  - CurrentAccounts
  - Payments
  - CashAndBank
  - Expenses
  - Common

ECommerce.Persistence
- Accounting
  - Configurations
  - Repositories

ECommerce.API
- Controllers
  - Accounting

Tests
- Accounting
  - Unit
  - Integration

Use namespaces consistent with this separation.

The Accounting module must not redefine:

- Product
- ProductVariant
- StockMovement
- Warehouse
- The existing e-commerce Order or OrderItem

# 3. CORE ACCOUNTING PRINCIPLE

The system must work according to the following separation of responsibilities.

StockMovement:

Represents physical stock quantity increases and decreases.

PurchaseInvoice:

Represents the cost, discount, VAT, and supplier debt of stock whose physical entry has already been recorded.

SalesInvoice:

Represents the optional internal invoice for an AccountingSalesOrder. It preserves invoice and CurrentAccount snapshots but never owns physical stock or a second receivable effect.

AccountingSalesOrder:

Represents the Accounting sales document, uses CurrentAccountId, receives ProductVariant rows directly from Accounting, and owns the atomic posting workflow.

Existing e-commerce Order:

Remains exclusive to authenticated User/cart checkout and is not used by manually entered Accounting sales.

InventoryCostLayer:

Tracks which portion of the remaining stock was purchased at which cost. It is not the source of stock quantity.

CurrentAccountTransaction:

Represents customer and supplier debit/credit movements.

FinancialTransaction:

Represents cash and bank movements.

# 4. DOCUMENT STATUSES

Purchase invoices and sales invoices must support at least:

- Draft
- Posted
- Cancelled

Draft:

- Can be edited.
- Does not create a stock movement.
- Does not create or consume a CostLayer.
- Does not create a current account transaction.
- Does not post an AccountingSalesOrder.

Posted:

- Represents a finalized accounting document.
- Cannot be edited through the normal update flow.
- Produces the relevant accounting effects.

Cancelled:

- Must not be physically deleted.
- Must store cancellation date, cancelling user, and cancellation reason.
- Must create the required reversing accounting movements.
- Must not directly manipulate stock.
- Any future stock reversal must use the existing StockMovement infrastructure; Accounting sales must not use the e-commerce Order cancellation or return workflow.

Do not hard-delete posted documents.

# 5. PURCHASE INVOICE LOGIC

There will be no separate GoodsReceipt or goods acceptance document.

Physical stock entries will already have been created by users through the existing StockMovement mechanism.

Correct workflow:

1. Product or ProductVariant already exists in the project.
2. The user creates a positive StockMovement using the existing stock movement infrastructure.
3. Physical stock increases.
4. A purchase invoice is created later.
5. The purchase invoice is matched with previously created positive StockMovement records.
6. The purchase invoice does not increase stock again.
7. The purchase invoice assigns cost, discount, and VAT information to the related stock quantities.
8. CostLayer records are created or finalized.
9. A supplier debt/current account transaction is created.

ABSOLUTE RULE:

Posting a PurchaseInvoice must never create a new StockMovement.

The following behavior is forbidden:

PurchaseInvoice.Post()
→ Create StockMovement + quantity

Correct behavior:

PurchaseInvoice.Post()
→ Stock quantity remains unchanged
→ Existing positive StockMovement records are linked
→ Costs are calculated
→ CostLayer records are created
→ Supplier current account transaction is created

Create an Accounting-module mapping table such as:

`PurchaseInvoiceStockAllocation`

Suggested fields:

- Id
- PurchaseInvoiceLineId
- StockMovementId
- AllocatedQuantity
- CreatedAt

Do not add fields to StockMovement merely to simplify this relationship.

A StockMovement may be partially allocated across multiple purchase invoice lines.

A purchase invoice line may also obtain quantity from multiple StockMovement records.

Example:

- First stock entry: +10
- Second stock entry: +15
- Purchase invoice line quantity: 20

Allocation:

- 10 units from the first movement
- 10 units from the second movement

The second movement still has 5 units available for cost allocation.

Calculation:

AvailableForCostAllocation =
Positive stock movement quantity
- Total previously allocated quantity

The same quantity must not be costed twice.

Only eligible positive stock entry types may be linked to purchase invoices.

Do not automatically allow movement types such as:

- Sales return
- Inventory count surplus
- Transfer-in
- Other non-purchase entries

Inspect the existing StockMovementType values and explicitly determine which movement types are eligible.

# 6. PURCHASE INVOICE ENTITIES

Evaluate at least the following structures:

- PurchaseInvoice
- PurchaseInvoiceLine
- PurchaseInvoiceStockAllocation
- PurchaseInvoiceExpense
- InventoryCostLayer
- ProductVariantCostHistory

PurchaseInvoice should contain at least:

- Id
- CurrentAccountId
- InvoiceNumber
- InvoiceDate
- DueDate
- CurrencyCode
- ExchangeRate
- Status
- Description

Invoice totals:

- SubtotalExcludingVat
- SubtotalIncludingVat

- LineDiscountTotalExcludingVat
- LineDiscountTotalIncludingVat

- InvoiceDiscountTotalExcludingVat
- InvoiceDiscountTotalIncludingVat

- TotalDiscountExcludingVat
- TotalDiscountIncludingVat

- NetAmountExcludingVat
- VatTotal
- GrandTotalIncludingVat

Purchase-specific totals:

- TotalAllocatedExpenseExcludingVat
- TotalAllocatedExpenseIncludingVat

- TotalFinalCostExcludingVat
- TotalFinalCostIncludingVat

Payment totals:

- PaidAmount
- RemainingAmount

Audit fields:

- CreatedBy
- CreatedAt
- UpdatedBy
- UpdatedAt
- PostedBy
- PostedAt
- CancelledBy
- CancelledAt
- CancellationReason

The same supplier invoice number must not be saved twice.

Create a unique constraint for:

CurrentAccountId + InvoiceNumber

PurchaseInvoiceLine should contain at least:

Identity:

- Id
- PurchaseInvoiceId
- ProductVariantId

Snapshot fields:

- ProductName
- VariantName
- SKU
- Barcode

Quantity and units:

- PurchaseQuantity
- UnitOfMeasure
- UnitsPerPurchaseUnit
- StockQuantity

Price:

- PriceEntryMode
- EnteredUnitPrice
- UnitPriceExcludingVat
- UnitPriceIncludingVat
- VatRate

Discount input:

- LineDiscountType
- LineDiscountValue
- LineDiscountTaxBasis
- LineDiscountUnitBasis

Calculated discount:

- LineDiscountAmountExcludingVat
- LineDiscountAmountIncludingVat
- InvoiceDiscountShareExcludingVat
- InvoiceDiscountShareIncludingVat
- TotalDiscountAmountExcludingVat
- TotalDiscountAmountIncludingVat

Calculated amounts:

- GrossAmountExcludingVat
- GrossAmountIncludingVat
- NetAmountExcludingVat
- VatAmount
- TotalAmountIncludingVat

Allocated expense:

- AllocatedExpenseExcludingVat
- AllocatedExpenseIncludingVat

Final cost:

- FinalTotalCostExcludingVat
- FinalTotalCostIncludingVat
- FinalUnitCostExcludingVat
- FinalUnitCostIncludingVat

Unit logic:

StockQuantity =
PurchaseQuantity × UnitsPerPurchaseUnit

Example:

- PurchaseQuantity = 5 boxes
- UnitsPerPurchaseUnit = 12 units
- StockQuantity = 60 units

The invoice may contain:

5 boxes × 1,200 TRY = 6,000 TRY

Final stock unit cost:

FinalUnitCostExcludingVat =
FinalTotalCostExcludingVat / StockQuantity

# 7. ACCOUNTING SALES LOGIC

Approved architectural decision:

1. The existing e-commerce Order remains exclusive to authenticated User/cart checkout.
2. AccountingSalesOrder is the Accounting sales document.
3. AccountingSalesOrder uses CurrentAccountId and never requires UserId.
4. AccountingSalesOrderItem uses ProductVariant rows provided directly by Accounting.
5. AccountingSalesOrder creates stock-out through the existing StockMovement infrastructure.
6. SalesInvoice does not directly create StockMovement.
7. SalesInvoice is optional and belongs to AccountingSalesOrder.
8. Posting AccountingSalesOrder creates exactly one customer receivable when the final receivable total is positive; a zero-total free sale creates no zero-valued ledger row.
9. External e-invoice integration will be implemented later and is separate from internal SalesInvoice creation.

AccountingSalesOrder must not require or read:

- UserId
- Cart or CartItem
- The e-commerce Address entity
- The existing e-commerce Order or OrderItem workflow

The project has no Warehouse entity. For this milestone, all physical stock is treated as belonging to one implicit warehouse. AccountingSalesOrderItem, SalesInvoiceLine, CostLayerConsumption, and their API inputs do not contain WarehouseId.

Finalized commercial rules:

- ProductVariant keeps its existing `Price > 0` invariant.
- Purchase and Accounting sales line price/cost input is optional and defaults to zero.
- Accounting documents copy Product, ProductVariant, SKU, and barcode snapshots when a line is first created.
- Existing line identity snapshots cannot be edited. Quantity, unit, price, VAT, and discount fields may be changed only on the Draft accounting line and must never update Product or ProductVariant master data.
- Accounting invoices use TRY and exchange rate 1.
- Shipping defaults to zero. `ShippingPayer.Customer` adds the VAT-free shipping amount to the customer receivable; `ShippingPayer.Seller` does not. Shipping is excluded from FIFO cost and gross profit.
- A free sale may use zero unit price and `CreateInvoice = false`; posting still creates the physical stock-out and FIFO consumption. Gross profit may therefore be negative, while zero-net profit margin is returned as zero.

Physical stock-out rules:

- Existing Product, ProductVariant, and StockMovement remain the physical stock infrastructure.
- `StockMovementType.AccountingSale = 22` is the approved workflow-owned negative Out movement type.
- Existing `StockMovementType.Sale` remains exclusive to e-commerce Order checkout and continues to require OrderId.
- AccountingSale does not reference the e-commerce Order.
- AccountingSalesOrder posting creates AccountingSale movements only through ProductVariant's existing stock movement aggregate method.
- Accounting code must not directly update ProductVariant.Stock.
- No AccountingStockMovement or second stock table may be created.
- StockMovement must not receive an AccountingSalesOrderId field.
- `AccountingSalesOrderStockMovement` links each AccountingSalesOrderItem to every StockMovement created for it.

Draft AccountingSalesOrder:

- Creates no StockMovement.
- Does not reduce stock.
- Does not consume CostLayer.
- Creates no CurrentAccountTransaction.
- May exist with or without its optional SalesInvoice.

Posted AccountingSalesOrder:

1. Validates Draft status.
2. Recalculates all line and header amounts on the API.
3. Validates an active Customer or CustomerAndSupplier CurrentAccount.
4. Validates every ProductVariant supplied directly by the Accounting request.
5. Validates available physical stock from the existing stock infrastructure.
6. Creates negative AccountingSale StockMovement records.
7. Creates AccountingSalesOrderStockMovement mappings.
8. Consumes InventoryCostLayer records by deterministic FIFO.
9. Calculates CostOfGoodsSold and profitability only from CostLayerConsumption records.
10. Creates exactly one customer receivable sourced primarily from AccountingSalesOrder when GrandTotalIncludingVat is positive; creates none when it is zero.
11. Marks the AccountingSalesOrder and, when applicable, its linked SalesInvoice as Posted.
12. Commits every effect atomically.

Posting is idempotent. A retry must not create another StockMovement, mapping, CostLayerConsumption, CurrentAccountTransaction, AccountingSalesOrder, or SalesInvoice.

Supported invoice workflows:

- `CreateInvoice = false`: create AccountingSalesOrder without SalesInvoice; posting still reduces stock and creates a receivable only when the final receivable total is positive.
- `CreateInvoice = true`: create exactly one linked SalesInvoice from the same trusted rows and totals; no duplicate stock or receivable effect is allowed.
- A SalesInvoice may be created later from an existing AccountingSalesOrder; it must not repeat stock or receivable effects.
- Direct SalesInvoice entry creates exactly one linked Draft AccountingSalesOrder from its ProductVariant rows and must be idempotent.
- One AccountingSalesOrder has zero or one SalesInvoice. Every SalesInvoice belongs to exactly one AccountingSalesOrder.

# 8. ACCOUNTING SALES ENTITIES

Evaluate at least:

- AccountingSalesOrder
- AccountingSalesOrderItem
- AccountingSalesOrderStockMovement
- SalesInvoice
- SalesInvoiceLine
- CostLayerConsumption

AccountingSalesOrder should contain at least:

- Id
- OrderNumber
- CurrentAccountId
- Status
- SubTotal
- DiscountTotal
- ShippingTotal
- TaxTotal
- GrandTotal
- PaidAmount
- RemainingAmount
- TotalCostOfGoodsSold
- GrossProfitExcludingVat
- GrossProfitMargin
- CurrentAccountNameSnapshot
- TaxNumberSnapshot
- TaxOfficeSnapshot
- PhoneNumberSnapshot
- EmailSnapshot
- AddressSnapshot
- PostedAt
- CancelledAt
- Existing audit fields

AccountingSalesOrder supports Draft, Posted, and Cancelled states. Cancellation and reversal behavior is outside this milestone; no cancellation command or reversing workflow is implemented here.

AccountingSalesOrderItem should contain at least:

- Id
- AccountingSalesOrderId
- ProductId
- ProductVariantId
- ProductNameSnapshot
- VariantNameSnapshot
- SkuSnapshot
- Quantity
- UnitPriceExcludingVat
- UnitPriceIncludingVat
- LineDiscountAmountExcludingVat
- LineDiscountAmountIncludingVat
- InvoiceDiscountShareExcludingVat
- InvoiceDiscountShareIncludingVat
- VatRate
- VatAmount
- NetAmountExcludingVat
- TotalAmountIncludingVat
- CostOfGoodsSold
- GrossProfitExcludingVat
- GrossProfitMargin

ProductVariantId and Quantity come directly from the Accounting request. WarehouseId is intentionally absent under the approved single implicit warehouse decision.

SalesInvoice should contain at least:

- Id
- AccountingSalesOrderId
- CurrentAccountId
- InvoiceNumber
- InvoiceDate
- DueDate
- CurrencyCode
- ExchangeRate
- Status
- Description
- CurrentAccountNameSnapshot
- TaxNumberSnapshot
- TaxOfficeSnapshot
- PhoneNumberSnapshot
- EmailSnapshot
- AddressSnapshot
- All common invoice totals
- TotalCostOfGoodsSold
- GrossProfitExcludingVat
- GrossProfitMargin
- PaidAmount
- RemainingAmount
- Existing audit fields

SalesInvoiceLine preserves the trusted product, variant, SKU, and barcode identity snapshots copied when its AccountingSalesOrderItem source is first created. Its Draft commercial values may be changed without changing those identity snapshots or the Product/ProductVariant master. It never maps to the existing e-commerce OrderItem.

Changing CurrentAccount, Product, or ProductVariant after posting must not change posted AccountingSalesOrder or SalesInvoice snapshots.

# 9. REQUIRED HEADER TOTALS FOR EVERY INVOICE

Both PurchaseInvoice and SalesInvoice must contain invoice-level totals calculated from their lines.

These totals must not be trusted when sent by the frontend.

The frontend may send raw inputs and display a preview, but the API must recalculate every invoice line and every invoice-level total.

Common invoice total fields:

- SubtotalExcludingVat
- SubtotalIncludingVat

- LineDiscountTotalExcludingVat
- LineDiscountTotalIncludingVat

- InvoiceDiscountTotalExcludingVat
- InvoiceDiscountTotalIncludingVat

- TotalDiscountExcludingVat
- TotalDiscountIncludingVat

- NetAmountExcludingVat
- VatTotal
- GrandTotalIncludingVat

- PaidAmount
- RemainingAmount

Definitions:

SubtotalExcludingVat:

The gross total of all invoice lines excluding VAT before any discount is applied.

SubtotalIncludingVat:

The gross total of all invoice lines including VAT before any discount is applied.

LineDiscountTotal:

The total of discounts entered directly on invoice lines.

InvoiceDiscountTotal:

The total invoice-level discount distributed across invoice lines.

TotalDiscount:

TotalDiscountExcludingVat =
LineDiscountTotalExcludingVat
+ InvoiceDiscountTotalExcludingVat

TotalDiscountIncludingVat =
LineDiscountTotalIncludingVat
+ InvoiceDiscountTotalIncludingVat

NetAmountExcludingVat:

NetAmountExcludingVat =
SubtotalExcludingVat
- TotalDiscountExcludingVat

VatTotal:

The total VAT calculated after discounts across all lines.

GrandTotalIncludingVat:

GrandTotalIncludingVat =
NetAmountExcludingVat
+ VatTotal

RemainingAmount:

RemainingAmount =
GrandTotalIncludingVat
- PaidAmount

PaidAmount and RemainingAmount must not be updated arbitrarily.

They must be calculated from PaymentAllocation records or safely synchronized with those records inside a transaction.

Invoice-level totals must exactly match the sum of invoice line values:

Invoice.SubtotalExcludingVat
=
SUM(Line.GrossAmountExcludingVat)

Invoice.SubtotalIncludingVat
=
SUM(Line.GrossAmountIncludingVat)

Invoice.LineDiscountTotalExcludingVat
=
SUM(Line.LineDiscountAmountExcludingVat)

Invoice.InvoiceDiscountTotalExcludingVat
=
SUM(Line.InvoiceDiscountShareExcludingVat)

Invoice.TotalDiscountExcludingVat
=
SUM(Line.TotalDiscountAmountExcludingVat)

Invoice.NetAmountExcludingVat
=
SUM(Line.NetAmountExcludingVat)

Invoice.VatTotal
=
SUM(Line.VatAmount)

Invoice.GrandTotalIncludingVat
=
SUM(Line.TotalAmountIncludingVat)

No rounding discrepancy may remain between invoice header totals and line totals.

# 10. PURCHASE-INVOICE-SPECIFIC COST TOTALS

PurchaseInvoice must additionally contain:

- TotalAllocatedExpenseExcludingVat
- TotalAllocatedExpenseIncludingVat
- TotalFinalCostExcludingVat
- TotalFinalCostIncludingVat

Final line cost excluding VAT:

FinalTotalCostExcludingVat =
GrossAmountExcludingVat
- LineDiscountAmountExcludingVat
- InvoiceDiscountShareExcludingVat
+ AllocatedExpenseExcludingVat

Final line cost including VAT:

FinalTotalCostIncludingVat =
GrossAmountIncludingVat
- LineDiscountAmountIncludingVat
- InvoiceDiscountShareIncludingVat
+ AllocatedExpenseIncludingVat

Final unit cost:

FinalUnitCostExcludingVat =
FinalTotalCostExcludingVat / StockQuantity

FinalUnitCostIncludingVat =
FinalTotalCostIncludingVat / StockQuantity

Purchase invoice totals:

PurchaseInvoice.TotalAllocatedExpenseExcludingVat
=
SUM(PurchaseInvoiceLine.AllocatedExpenseExcludingVat)

PurchaseInvoice.TotalFinalCostExcludingVat
=
SUM(PurchaseInvoiceLine.FinalTotalCostExcludingVat)

Use the final cost excluding VAT as the primary stock valuation cost when creating CostLayer records.

The VAT-included cost may also be stored for display and reporting.

# 11. SALES-INVOICE-SPECIFIC COST AND PROFIT TOTALS

SalesInvoice must additionally contain:

- TotalCostOfGoodsSold
- GrossProfitExcludingVat
- GrossProfitMargin

TotalCostOfGoodsSold must not be an assumed cost calculated directly by SalesInvoice.

It must be derived from:

- The AccountingSalesOrder
- AccountingSale StockMovement records created when AccountingSalesOrder is posted
- FIFO CostLayer records consumed because of those stock movements

Formula:

TotalCostOfGoodsSold =
SUM(CostLayerConsumption.TotalCost)

Gross profit:

GrossProfitExcludingVat =
NetAmountExcludingVat
- TotalCostOfGoodsSold

Profit margin:

GrossProfitMargin =
GrossProfitExcludingVat
/ NetAmountExcludingVat
× 100

Prevent division by zero when NetAmountExcludingVat is zero.

Line-level calculation:

SalesInvoiceLine.CostOfGoodsSold
=
Sum of CostLayer costs consumed by the related AccountingSalesOrderItem and mapped StockMovement records

SalesInvoiceLine.GrossProfitExcludingVat
=
SalesInvoiceLine.NetAmountExcludingVat
- SalesInvoiceLine.CostOfGoodsSold

Invoice totals:

SalesInvoice.TotalCostOfGoodsSold
=
SUM(SalesInvoiceLine.CostOfGoodsSold)

SalesInvoice.GrossProfitExcludingVat
=
SUM(SalesInvoiceLine.GrossProfitExcludingVat)

# 12. DISCOUNT SYSTEM

Purchase invoices and sales invoices must use the same centralized discount infrastructure.

Discount scope:

- Line
- Invoice

Discount value types:

- Percentage
- FixedPerUnit
- FixedLineTotal
- FixedInvoiceTotal

When FixedPerUnit is used, the discount unit basis must be specified:

- PurchaseUnit or SaleUnit
- StockUnit

Example:

There are 5 boxes with 12 units per box.

For a 5 TRY discount:

- PurchaseUnit selected: 5 boxes × 5 TRY = 25 TRY
- StockUnit selected: 60 units × 5 TRY = 300 TRY

Discount VAT basis:

- ExcludingVat
- IncludingVat

The user must be able to enter discounts such as:

- 10% discount on the VAT-exclusive amount
- 10% discount on the VAT-inclusive amount
- 5 TRY per unit discount on the VAT-exclusive amount
- 100 TRY discount from the VAT-inclusive line total
- 5% invoice-level discount
- 500 TRY fixed invoice-level discount

Store the user-entered discount definition separately from the calculated discount amounts.

Input fields:

- DiscountScope
- DiscountType
- DiscountValue
- DiscountTaxBasis
- DiscountUnitBasis

Calculated fields:

- DiscountAmountExcludingVat
- DiscountAmountIncludingVat

Discount validation:

- Percentage must be between 0 and 100.
- A fixed discount cannot exceed the applicable base.
- Negative discounts must not be accepted.
- The resulting line net amount cannot be negative.

# 13. DISTRIBUTION OF INVOICE-LEVEL DISCOUNTS

A percentage invoice-level discount may be applied to all eligible lines using the same percentage.

Example:

- Product A: 1,000 TRY
- Product B: 500 TRY
- Invoice discount: 10%

Distribution:

- Product A: 100 TRY
- Product B: 50 TRY

A fixed invoice-level discount must not be distributed as an equal fixed amount to every line.

It must be distributed proportionally according to eligible line amounts.

Example:

- Product A: 1,000 TRY
- Product B: 500 TRY
- Invoice-level discount: 300 TRY

Distribution:

- Product A: 200 TRY
- Product B: 100 TRY

Formula:

LineInvoiceDiscountShare =
InvoiceDiscountAmount
× LineEligibleBase
/ InvoiceEligibleBaseTotal

It must be possible to specify whether a line is eligible for invoice-level discount:

- IsInvoiceDiscountEligible

Any rounding difference must be assigned deterministically to the final eligible line so that:

SUM(Line.InvoiceDiscountShare)
=
InvoiceDiscountAmount

# 14. VAT-INCLUSIVE AND VAT-EXCLUSIVE CALCULATION

Prices must support two input modes:

- ExcludingVat
- IncludingVat

Use an enum such as:

PriceEntryMode

The user sends only one price and the PriceEntryMode.

The API calculates both VAT-exclusive and VAT-inclusive equivalents.

When the VAT-exclusive unit price is entered:

UnitPriceIncludingVat =
UnitPriceExcludingVat × (1 + VatRate / 100)

When the VAT-inclusive unit price is entered:

UnitPriceExcludingVat =
UnitPriceIncludingVat / (1 + VatRate / 100)

VAT:

VatAmount =
NetAmountIncludingVat - NetAmountExcludingVat

Store the following snapshot values separately on every invoice line:

- UnitPriceExcludingVat
- UnitPriceIncludingVat
- GrossAmountExcludingVat
- GrossAmountIncludingVat
- DiscountAmountExcludingVat
- DiscountAmountIncludingVat
- NetAmountExcludingVat
- VatAmount
- TotalAmountIncludingVat

A single invoice may contain lines with different VAT rates.

VAT must be calculated from the post-discount line base.

General calculation order:

1. Quantity × unit price
2. Line discount
3. Allocated invoice-level discount
4. Net VAT-exclusive base
5. VAT
6. VAT-inclusive total

# 15. CENTRALIZED CALCULATION ENGINE

Do not spread calculation logic across controllers or frontend components.

Create centralized calculation infrastructure shared by purchase invoices and sales invoices.

Possible structures:

- IInvoiceCalculationService
- InvoiceCalculationInput
- InvoiceLineCalculationInput
- InvoiceCalculationResult
- InvoiceLineCalculationResult
- Money or InvoiceMoney value object
- DiscountDefinition value object
- VAT calculation service or value object

The frontend should only send raw input values:

- ProductVariantId
- Quantity
- UnitsPerUnit
- EnteredUnitPrice
- PriceEntryMode
- VatRate
- Line discount information
- Invoice-level discount information
- Expense information

The frontend may calculate preview values.

However, the API must not trust frontend-calculated values such as:

- Subtotal
- Discount total
- VAT total
- Grand total
- Cost
- Profit

The API must recalculate all values.

# 16. MONEY PRECISION AND ROUNDING

Use `decimal` for all monetary calculations.

Do not use `double` or `float`.

Suggested database precision:

- UnitPrice: decimal(18,4)
- UnitCost: decimal(18,4)
- ExchangeRate: decimal(18,6)
- Quantity: decimal(18,4)
- Percentage: decimal(9,4)
- Invoice totals: decimal(18,2)

Create one centralized rounding policy.

For example:

MidpointRounding.AwayFromZero

Unit prices and costs may use four decimal places.

Invoice totals may use two decimal places.

Rounding differences must be distributed deterministically to the final eligible line.

# 17. COSTLAYER SYSTEM

The same ProductVariant may be purchased at different costs on different dates.

Example:

- 10 units × 100 TRY
- 5 units sold
- 15 units × 140 TRY

Remaining stock:

- 5 units × 100 TRY
- 15 units × 140 TRY

Track this using `InventoryCostLayer` inside the Accounting module.

Suggested InventoryCostLayer fields:

- Id
- ProductVariantId
- StockMovementId
- PurchaseInvoiceLineId
- OriginalQuantity
- RemainingQuantity
- UnitCostExcludingVat
- UnitCostIncludingVat
- TotalCostExcludingVat
- TotalCostIncludingVat
- CostDate
- Status
- CreatedAt

WarehouseId is intentionally omitted because the approved scope uses the project's single implicit warehouse.

CostLayer is not the source of stock quantity.

The existing StockMovement system remains the source of stock quantity.

CostLayer only answers:

“How much stock remains from this acquisition, and what was its cost?”

When PurchaseInvoice is posted:

- No new StockMovement is created.
- The invoice is linked to selected positive StockMovement quantities.
- A CostLayer is created for the allocated quantity.
- Both VAT-exclusive and VAT-inclusive costs are recorded.
- The purchase line price/cost may be omitted and defaults to zero.
- A zero-cost posted line still creates its approved zero-cost layer, but creates no zero-valued supplier debt transaction.

Positive stock created with a new ProductVariant receives exactly one explicit `OpeningBalance` InventoryCostLayer in the same unit of work. Its opening VAT-exclusive cost is optional and defaults to zero; when VAT-inclusive cost is omitted it defaults to the VAT-exclusive value. A later opening-cost update changes only the layer's `RemainingQuantity` valuation and never rewrites existing CostLayerConsumption records.

# 18. FIFO COSTLAYER CONSUMPTION

Use FIFO for sales.

The oldest open CostLayer with a positive RemainingQuantity must be consumed first.

Ordering must be deterministic:

- CostDate ASC
- CreatedAt ASC
- Id ASC

Example:

- CostLayer 1: 5 units × 100 TRY
- CostLayer 2: 15 units × 140 TRY
- Sale: 8 units

Consumption:

- 5 units from Layer 1 × 100 TRY
- 3 units from Layer 2 × 140 TRY

Total cost of goods sold:

500 TRY + 420 TRY = 920 TRY

A single sale may consume multiple CostLayers.

Store consumption records in a separate table:

`CostLayerConsumption`

Suggested fields:

- Id
- InventoryCostLayerId
- AccountingSalesOrderId
- AccountingSalesOrderItemId
- StockMovementId
- Quantity
- UnitCost
- TotalCost
- CreatedAt

CostLayerConsumption must allow the system to answer:

- Which purchases supplied this sale?
- What was the cost of the sale?
- How many units were consumed from each layer?
- Which cost should be restored in case of a return?

FIFO consumption must not be performed directly by SalesInvoice.

It must be associated with the AccountingSalesOrderItem and the AccountingSale StockMovement connected through AccountingSalesOrderStockMovement.

# 19. STOCK WITHOUT FINALIZED COST

Approved behavior:

- Positive opening stock is never left without a CostLayer.
- When no cost is supplied, its explicit OpeningBalance layer uses zero cost.
- Accounting sales may consume that layer through normal deterministic FIFO.
- A later opening-cost update applies only to the unconsumed RemainingQuantity.
- Past CostLayerConsumption unit costs, posted COGS, and posted profitability are immutable and are not retrospectively revalued.
- This is an approved zero-cost policy, not use of Product/ProductVariant current price or latest purchase price as sales cost.

# 20. PRODUCT VARIANT COST HISTORY

In addition to CostLayer, a reporting-oriented cost history may be created:

`ProductVariantCostHistory`

Suggested fields:

- Id
- ProductVariantId
- PreviousCostExcludingVat
- NewCostExcludingVat
- PreviousCostIncludingVat
- NewCostIncludingVat
- ValidFrom
- ValidTo
- OpeningStockQuantity
- ClosingStockQuantity
- SourceType
- SourceId
- CreatedAt

WarehouseId is intentionally omitted under the approved single implicit warehouse decision.

When a new cost becomes effective:

1. Find the previous active cost history record.
2. Set the previous record’s ValidTo to the new cost date.
3. Store the current stock quantity in the previous record’s ClosingStockQuantity.
4. Create a new history record.
5. Store the current stock quantity in the new record’s OpeningStockQuantity.

This history table must not be the primary cost source.

Primary cost sources are:

- PurchaseInvoiceLine
- InventoryCostLayer
- CostLayerConsumption

# 21. CURRENT ACCOUNT

Approved business decision:

“CurrentAccount is the single customer/supplier master record. Basic identity, communication, tax, and address information are stored directly in CurrentAccount. Separate Supplier and CurrentAccountAddress entities are not used.”

CurrentAccount is the only Accounting party master for customers, suppliers, and parties with both roles. Its type values are Customer, Supplier, and CustomerAndSupplier. Code, name/title, optional trade and national identity information, tax information, phone, email, one current address, active status, optional existing UserId link, and audit fields are stored directly on CurrentAccount.

PurchaseInvoice references CurrentAccountId and accepts only an active Supplier or CustomerAndSupplier account. SalesInvoice references CurrentAccountId and accepts only an active Customer or CustomerAndSupplier account. CurrentAccountTransaction references CurrentAccountId directly.

No Supplier, SupplierProfile, SupplierAddress, CurrentAccountAddress, CustomerAccount, or Accounting CustomerAddress entity/table is used. Invoice snapshot fields remain on invoices and are refreshed at posting so later CurrentAccount changes cannot alter posted history.

CurrentAccountTransaction should consider at least:

- CurrentAccountId
- TransactionType
- DebitAmount
- CreditAmount
- CurrencyCode
- ExchangeRate
- TransactionDate
- DueDate
- SourceType
- SourceId
- Description

Posting a purchase invoice:

- Creates supplier debt when GrandTotalIncludingVat is positive.
- Creates no zero-valued supplier debt transaction when the total is zero.

Posting an AccountingSalesOrder:

- Creates exactly one customer receivable sourced from AccountingSalesOrder when GrandTotalIncludingVat is positive.
- Creates no zero-valued receivable transaction when the total is zero.
- A linked SalesInvoice is a related document and must not create a second receivable.

Current account balances must not be modified arbitrarily.

CurrentAccountTransaction records must be the primary source.

If a cached balance snapshot is stored for performance, it must be synchronized atomically with transaction records.

# 22. PAYMENTS AND COLLECTIONS

An invoice and a payment must not be the same record.

An invoice may:

- Be on credit
- Be partially paid
- Be settled through multiple payments

A payment may:

- Be allocated across multiple invoices

Evaluate:

- Payment
- PaymentAllocation

Payment types:

- CustomerCollection
- SupplierPayment

PaymentAllocation fields:

- PaymentId
- InvoiceType
- InvoiceId
- AllocatedAmount

Example:

A 10,000 TRY supplier payment:

- 6,000 TRY → Purchase invoice A
- 4,000 TRY → Purchase invoice B

PaidAmount and RemainingAmount must be calculated from these allocations.

# 23. CASH, BANK, AND FINANCIAL TRANSACTIONS

Evaluate:

- CashAccount
- BankAccount
- FinancialTransaction

Financial transaction types may include:

- CustomerCollection
- SupplierPayment
- CashIn
- CashOut
- BankTransferIn
- BankTransferOut
- POSCollection
- BankCommission
- MarketplaceCommission
- ExpensePayment
- Refund

Cash or bank balances must not be directly modified arbitrarily.

FinancialTransaction records must be the primary source.

# 24. EXPENSES AND PURCHASE COST ALLOCATION

Evaluate:

- Expense
- ExpenseCategory
- PurchaseInvoiceExpense

Example expense types:

- Shipping
- Transportation
- Insurance
- Customs
- Advertising
- Rent
- Software
- Bank commission
- Marketplace commission
- Other

Some expenses may remain general business expenses.

Some purchase-related expenses may be allocated to product costs:

- Transportation
- Shipping
- Customs
- Insurance

Allocation methods:

- Proportional to VAT-exclusive line amount
- Proportional to quantity
- Manual allocation

For the first version, proportional allocation based on line amount may be sufficient.

Any rounding difference must be assigned to the final eligible line so that the total allocated expense equals the invoice expense total.

# 25. CANCELLATION AND REVERSING ENTRIES

Do not hard-delete posted invoices.

Purchase invoice cancellation:

- Must not create a new StockMovement.
- Must reverse the supplier current account debt.
- Must safely reverse or invalidate related CostLayer records.

If a CostLayer has already been consumed by sales, purchase invoice cancellation becomes complex.

Do not make assumptions.

Analyze and present these options:

- Block cancellation when the CostLayer has been consumed.
- Create a cost adjustment document.
- Create reversing cost entries.

Ask for my approval before implementing one of these behaviors.

Accounting sales cancellation:

- Is outside the revised Accounting Sales milestone.
- Must not use or modify the existing e-commerce Order cancellation or return workflow.
- A future approved workflow must reverse AccountingSale stock effects through the existing StockMovement infrastructure and reverse the AccountingSalesOrder receivable.
- SalesInvoice must not create an independent stock or receivable reversal.

# 26. TRANSACTIONS AND IDEMPOTENCY

PurchaseInvoice posting must occur within one transaction:

1. Validate invoice status.
2. Calculate invoice lines.
3. Calculate invoice header totals.
4. Validate StockMovement allocations.
5. Validate available allocation quantities.
6. Create CostLayer records.
7. Update cost history.
8. Create supplier current account transaction.
9. Mark the invoice as Posted.
10. Commit.

If any step fails, no partial records may remain.

AccountingSalesOrder posting must be handled as one safe business workflow:

1. Validate Draft status and idempotency state.
2. Recalculate AccountingSalesOrder items and header totals.
3. Validate the Customer or CustomerAndSupplier CurrentAccount.
4. Validate request-supplied ProductVariant rows and available physical stock.
5. Create negative AccountingSale StockMovement records through ProductVariant.
6. Create AccountingSalesOrderStockMovement mapping records.
7. Create FIFO CostLayerConsumption records.
8. Update InventoryCostLayer RemainingQuantity values.
9. Calculate item and order CostOfGoodsSold, gross profit, and margin.
10. When the final total is positive, create the customer receivable exactly once with AccountingSalesOrder as its source; when it is zero, create none.
11. Create or finalize the optional linked SalesInvoice without adding another stock or receivable effect.
12. Mark the AccountingSalesOrder and applicable SalesInvoice as Posted.
13. Commit.

The existing e-commerce Order use case and its transaction remain unchanged and do not participate in this Accounting transaction.

Posting commands must be idempotent.

Repeating the same command must not create:

- A second AccountingSalesOrder
- A second SalesInvoice for the same AccountingSalesOrder
- A second AccountingSale StockMovement or mapping
- A second current account transaction
- A second CostLayerConsumption
- A duplicated allocation

# 27. CONCURRENCY

The same stock entry must not be over-allocated concurrently by two purchase invoices.

The same CostLayer must not be over-consumed concurrently by two sales.

Inspect the project’s current concurrency approach.

Evaluate:

- RowVersion
- Optimistic concurrency
- Transaction isolation
- Database-level locking
- Unique indexes
- Database constraints

Ask for approval before changing the core StockMovement structure.

# 28. VALIDATION

The API must implement at least the following validations:

- Quantity must be greater than zero.
- UnitsPerPurchaseUnit must be greater than zero.
- StockQuantity must be calculated correctly.
- UnitPrice cannot be negative.
- VatRate cannot be negative.
- Percentage discount must be between 0 and 100.
- A fixed discount cannot exceed its base.
- Net amount cannot be negative.
- An invoice must contain at least one line.
- Duplicate product lines must follow an explicit project policy.
- Purchase allocation quantity cannot exceed eligible stock entry quantity.
- A StockMovement created for another ProductVariant cannot be linked to the wrong invoice line.
- Accounting sales requests must not accept WarehouseId in this single implicit warehouse milestone.
- AccountingSalesOrder CurrentAccountId must support Customer or CustomerAndSupplier.
- AccountingSalesOrderStockMovement must link a movement to the matching AccountingSalesOrderItem and ProductVariant.
- SalesInvoice cannot be posted twice.
- PurchaseInvoice cannot be posted twice.
- Posted documents cannot be changed through a normal update operation.
- InvoiceNumber is required and must have a valid maximum length.
- Invoice CurrentAccountId must exist and its CurrentAccountType must support the invoice role.
- Accounting invoice CurrencyCode must be TRY.
- Accounting invoice ExchangeRate must equal 1.
- Existing invoice line ProductVariantId, SKU, and barcode snapshots cannot be changed through commercial update operations.

# 29. REPORTING

Plan at least the following queries and reports:

- Purchase invoice list
- Purchase invoice detail
- Sales invoice list
- Sales invoice detail
- Positive stock movements without finalized cost
- Partially cost-allocated stock movements
- CostLayer list
- Remaining quantities by CostLayer
- Cost history by ProductVariant
- Stock valuation for the single implicit warehouse
- Stock cost by product or variant
- Profitability by sales invoice
- Profitability by AccountingSalesOrder
- Profitability by product
- Current account statement
- Customer receivables
- Supplier debts
- Overdue invoices
- Payments and collections
- Cash movements
- Bank movements
- Purchase VAT summary by VAT rate
- Sales VAT summary by VAT rate

# 30. API ENDPOINTS

Follow the project’s existing CQRS and MediatR style.

Example endpoints:

PurchaseInvoice:

- POST /api/accounting/purchase-invoices
- PUT /api/accounting/purchase-invoices/{id}
- GET /api/accounting/purchase-invoices
- GET /api/accounting/purchase-invoices/{id}
- POST /api/accounting/purchase-invoices/{id}/post
- POST /api/accounting/purchase-invoices/{id}/cancel
- GET /api/accounting/purchase-invoices/available-stock-movements

AccountingSalesOrder:

- POST /api/accounting/sales-orders
- PUT /api/accounting/sales-orders/{id}
- GET /api/accounting/sales-orders
- GET /api/accounting/sales-orders/{id}
- POST /api/accounting/sales-orders/{id}/lines
- PUT /api/accounting/sales-orders/{id}/lines/{lineId}
- DELETE /api/accounting/sales-orders/{id}/lines/{lineId}
- POST /api/accounting/sales-orders/{id}/post
- POST /api/accounting/sales-orders/{id}/invoice

SalesInvoice:

- POST /api/accounting/sales-invoices
- PUT /api/accounting/sales-invoices/{id}
- GET /api/accounting/sales-invoices
- GET /api/accounting/sales-invoices/{id}
- POST /api/accounting/sales-invoices/{id}/post

Sales cancellation endpoints are outside this milestone.

CostLayer:

- GET /api/accounting/cost-layers
- GET /api/accounting/cost-layers/by-variant/{variantId}
- GET /api/accounting/cost-history/by-variant/{variantId}

CurrentAccount:

- GET /api/accounting/current-accounts
- GET /api/accounting/current-accounts/{id}/statement

Payments:

- POST /api/accounting/payments
- GET /api/accounting/payments
- GET /api/accounting/payments/{id}

Adapt route names to the project’s existing controller and routing conventions.

# 31. AUDIT LOG AND SOURCE RELATIONSHIPS

The system must be able to determine:

- Who created the record?
- Who updated it?
- Who posted it?
- Who cancelled it?
- When was it created?
- When was it posted?
- What was the cancellation reason?
- Which stock movement was costed by which purchase invoice?
- Which optional SalesInvoice belongs to which AccountingSalesOrder?
- Which AccountingSale StockMovements were created for each AccountingSalesOrderItem?
- Which CostLayers were consumed by each AccountingSalesOrderItem?
- Which CurrentAccountTransaction came from which PurchaseInvoice or AccountingSalesOrder?
- Which payments were allocated to which invoices?

Use source relationships where appropriate:

- SourceType
- SourceId
- SourceLineId

However, if adding SourceType to a core project entity is required, ask for my approval first.

# 32. TESTS

Plan and implement the following unit and integration tests during the relevant phases.

## Purchase invoice tests

1. A Draft purchase invoice must not create StockMovement.
2. A Posted purchase invoice must not create StockMovement.
3. A Posted purchase invoice must link to existing positive StockMovement records.
4. The same stock quantity must not be allocated twice.
5. Partial allocation must be supported.
6. One invoice line must be able to link to multiple stock movements.
7. One stock movement must be able to be partially linked to multiple invoices.
8. A stock movement for the wrong variant must not be accepted.
9. The CurrentAccountId and InvoiceNumber unique constraint must work.
10. A Posted purchase invoice cannot be posted again.
11. CostLayer must be created with the correct quantity and cost.
12. The supplier current account transaction must be created exactly once.

## Sales invoice tests

13. A Draft AccountingSalesOrder must not create StockMovement.
14. A Draft AccountingSalesOrder must not create a customer receivable.
15. AccountingSalesOrder must not require UserId.
16. AccountingSalesOrder must not access a shopping cart.
17. ProductVariant rows must come directly from the Accounting request.
18. A Posted AccountingSalesOrder must create the expected negative AccountingSale StockMovement records.
19. Retrying posting must not create duplicate StockMovement records.
20. Accounting sales must not create the existing e-commerce Order.
21. The existing e-commerce Order code must remain unchanged.
22. CreateInvoice=false must create no SalesInvoice.
23. CreateInvoice=true must create exactly one SalesInvoice.
24. Creating SalesInvoice later must not repeat stock or receivable effects.
25. Direct SalesInvoice entry must create exactly one AccountingSalesOrder.
26. SalesInvoice must not directly create StockMovement.
27. One customer receivable must be created per positive-total Posted AccountingSalesOrder; a zero-total free sale must create none.
28. FIFO consumption and profitability must be correct.
29. A failure must roll back StockMovement, FIFO consumption, receivable, AccountingSalesOrder status, and SalesInvoice status.

## Calculation tests

30. VAT-exclusive prices must be calculated correctly.
31. VAT-inclusive prices must be calculated correctly.
32. Percentage line discounts must be calculated correctly.
33. FixedPerUnit discounts must be calculated correctly.
34. FixedLineTotal discounts must be calculated correctly.
35. Percentage invoice discounts must be distributed correctly.
36. Fixed invoice-level discounts must be distributed proportionally.
37. Fixed discounts on VAT-inclusive amounts must be separated correctly.
38. Discounts on VAT-exclusive amounts must be calculated correctly.
39. Rounding differences must be assigned to the final eligible line.
40. Line totals and invoice header totals must match.
41. TotalDiscount must be correct.
42. VatTotal must equal the sum of line VAT.
43. GrandTotalIncludingVat must equal the sum of line totals.

## CostLayer and FIFO tests

44. FIFO must consume the oldest open CostLayer first.
45. One sale must be able to consume multiple CostLayers.
46. RemainingQuantity must never become negative.
47. TotalCostOfGoodsSold must equal the sum of CostLayerConsumption totals.
48. Sales line cost must be calculated from related AccountingSalesOrderItem consumptions.
49. Gross profit must be calculated correctly.
50. Profit margin calculations must prevent division by zero.
51. Concurrent sales must not over-consume the same CostLayer.

## Transaction tests

52. If purchase invoice posting fails, no partial CostLayer records may remain.
53. If AccountingSalesOrder posting fails, no partial StockMovement, consumption, receivable, status, or invoice effect may remain.
54. If current account transaction creation fails, AccountingSalesOrder and SalesInvoice must not become Posted.
55. Repeating the same command must not create duplicate accounting records.

# 33. IMPLEMENTATION PHASES

Do not implement everything at once without control.

Use the following phased order:

Phase 1:
Analyze the existing project and prepare the integration plan.

Phase 2:
Create the Accounting folder structure and design shared enums and value objects.

Phase 3:
Create the centralized invoice, discount, and VAT calculation engine.

Phase 4:
Implement PurchaseInvoice and PurchaseInvoiceLine.

Phase 5:
Implement PurchaseInvoiceStockAllocation and integration with existing StockMovement records.

Phase 6:
Implement InventoryCostLayer and cost history.

Phase 7:
Implement the complete revised Accounting Sales milestone: AccountingSalesOrder, optional SalesInvoice, AccountingSale StockMovement mapping, customer receivable, FIFO, cost, profitability, APIs, and tests.

Phase 8:
Reserved; the former e-commerce Order integration phase was removed by the approved AccountingSalesOrder decision.

Phase 9:
Reserved; FIFO and sales cost calculations are included in the complete revised Accounting Sales milestone.

Phase 10:
Implement CurrentAccount and current account transactions.

Phase 11:
Implement Payment, collections, cash, and bank features.

Phase 12:
Implement expenses and allocation of purchase expenses to product costs.

Phase 13:
Implement reports.

Phase 14:
Implement integration tests and migrations.

At the end of every phase:

- List all newly created files.
- List all modified existing files.
- Explain why each existing file was modified.
- Provide build results.
- Provide test results.
- Ask before continuing when a critical decision is required.

# 34. REQUIRED FORMAT FOR YOUR FIRST RESPONSE

Do not write code in your first response.

Inspect the project and provide a report using the following format.

## A. Existing structures

Find the actual file paths, entities, and workflows for:

- Product
- ProductVariant
- Warehouse availability; if absent, confirm the approved single implicit warehouse boundary instead of inventing a Warehouse entity
- StockMovement
- StockMovementType
- Current stock calculation workflow
- The existing e-commerce Order/OrderItem/User/Cart boundary, only to prove that Accounting sales do not call or modify it
- ProductVariant's existing stock movement aggregate method that AccountingSalesOrder will reuse for AccountingSale
- Existing checkout Order tests that must remain unchanged as regression coverage
- Customer/User
- Repository infrastructure
- UnitOfWork
- Transaction management
- AuditableEntity
- Existing price, money, tax, and VAT fields
- Existing test structure

## B. Existing structures to reuse

For each existing structure, explain how it will be reused by the Accounting module.

## C. New Accounting structures to create

List the following in a table:

- Entity
- Enum
- Value object
- Repository interface
- Command
- Query
- Handler
- Controller
- Validation
- Database configuration
- Test

## D. Core project changes you believe are required

For every proposed change, provide:

- File name
- Proposed change
- Why is it required?
- Is there an alternative solution inside the Accounting module?
- Does it have a breaking impact?
- Does it require my approval?

Do not apply these changes without my permission.

## E. Unresolved business rules

Pay particular attention to:

- Can stock without finalized cost be sold?
- AccountingSalesOrder uses CurrentAccount snapshots and never requires the e-commerce Address entity.
- AccountingSalesOrder starts as Draft and physical stock decreases only when it is Posted.
- The existing e-commerce Order, Cart, reservation, cancellation, return, and transaction workflows are outside Accounting sales.
- AccountingSale StockMovement and AccountingSalesOrderStockMovement mapping changes are explicitly approved.
- CurrentAccount is the approved single customer/supplier master; no separate Supplier entity is used.
- This milestone uses one implicit warehouse and contains no WarehouseId.
- Are VAT rates fixed enums or dynamic data?
- Is negative stock allowed?

Do not make assumptions.

Present available options and their consequences.

## F. Proposed implementation plan

For every phase, provide:

- Tasks
- New files
- Existing file changes
- Tests
- Risks

FINAL AND ABSOLUTE RULES:

1. Existing StockMovement is the project’s only stock movement mechanism.
2. A PurchaseInvoice must never create a new StockMovement.
3. A SalesInvoice must never directly create StockMovement.
4. Existing e-commerce Order remains exclusive to User/cart checkout.
5. AccountingSalesOrder is the Accounting sales document, uses CurrentAccountId, and never requires UserId.
6. AccountingSalesOrderItem uses ProductVariant rows provided directly by Accounting.
7. AccountingSalesOrder creates AccountingSale stock-out through the existing StockMovement infrastructure.
8. AccountingSalesOrderStockMovement maps AccountingSalesOrderItem to StockMovement without adding an Accounting FK to StockMovement.
9. SalesInvoice is optional and belongs to AccountingSalesOrder.
10. Posting AccountingSalesOrder creates the customer receivable exactly once only for a positive final total; a zero-total free sale creates none.
11. External e-invoice integration is later and separate from internal SalesInvoice creation.
12. The approved milestone uses one implicit warehouse and no WarehouseId.
13. Product, ProductVariant, Warehouse, StockMovement, and the e-commerce Order/OrderItem structures must not be duplicated.
14. FIFO CostLayer consumption must be associated with the AccountingSalesOrderItem and its mapped AccountingSale StockMovement.
15. The Accounting module must remain in separate folders and namespaces as much as possible.
16. Invoice and AccountingSalesOrder totals must be calculated by the API from their rows.
17. Every invoice must contain subtotal, total discount, total VAT, net amount excluding VAT, and grand total including VAT.
18. ProductVariant.Price remains greater than zero, while Accounting purchase/sales line commercial price may be omitted and defaults to zero.
19. Existing invoice line ProductVariantId, SKU, and barcode snapshots are immutable; Draft commercial changes never update Product or ProductVariant master data.
20. Accounting invoice currency is TRY with exchange rate 1.
21. Optional shipping is customer-receivable only when paid by the customer and is excluded from VAT, FIFO cost, and gross profit.
22. Positive opening stock always has an explicit OpeningBalance CostLayer; omitted cost is zero and later revaluation changes only RemainingQuantity.
18. Purchase invoices must contain total final cost.
19. Accounting sales must contain FIFO cost of goods sold, gross profit, and profit margin.
20. Frontend-calculated totals must not be trusted.
21. All operations must comply with transaction, idempotency, concurrency, and audit requirements.

Yanıtlarını Türkçe olarak ver.

AGENT.md de yazdığım gibi her yere türkçe yorum satırı ekle.
