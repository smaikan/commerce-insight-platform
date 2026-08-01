# Tables, Filters, and Bulk Operations

## Table anatomy

Use one coherent data surface:

1. view/tab row when saved views or meaningful states exist;
2. search and supported filters;
3. selection/bulk bar when applicable;
4. column header with sorting only where documented;
5. compact rows;
6. pagination and result count.

Keep the entity name/identifier prominent. Align money and quantities right, dates consistently, and statuses in a stable column. Use thumbnails only when they speed recognition.

## Filters and URL state

- Model search, filter, sort, page, and page size in URL parameters.
- Reset `pageNumber` to 1 after filter changes.
- Debounce free-text search only when it reduces request churn without making state unclear.
- Provide a visible clear/reset action when filters are active.
- Distinguish an empty dataset from zero results under current filters.
- Never expose a filter or sort field missing from the endpoint contract.

## Row and bulk actions

- Make the row itself or its primary identifier navigate to detail.
- Keep one or two frequent inline actions at most; put rare actions in a labeled overflow menu.
- Do not hide destructive or lifecycle implications behind an unlabeled icon.
- Show selection checkboxes only when a real action consumes the selection.
- State whether selection covers the current page or a broader result set.
- Clear or reconcile invalid selection after filtering, paging, or mutations.
- Use an atomic bulk endpoint when atomic behavior is promised. Do not imply rollback for a client-side request loop.

Project examples:

- Product bulk creation exists; arbitrary bulk status/activation is not assumed.
- Stock movement bulk accepts at most 500 rows and is atomic.
- Other entities require their documented operations; do not infer bulk support from checkboxes.

## Entity-specific priorities

- Products: image, title/public ID/SKU, status, active/featured state, variants/stock summary only when authoritative, type/brand, updated context.
- Orders: order number, customer when provided, lifecycle status, total, payment state, created date.
- Stock movements: variant, direction/type, signed quantity, before/after, reason, source reference, date.
- Customers: public ID, identity, role, status, last login/created date; role/status changes can conflict.
- Returns: return number, order, type, status, refund total, dates, next valid action.
- Accounting: use report/document-specific columns; never one universal finance table.

Render only fields returned by the actual DTO. Do not calculate missing values from incomplete pages.

## States and density

- Loading: render a stable toolbar and a small number of row skeletons matching real columns.
- Empty: keep table context/filters visible and explain the valid next step.
- Error: preserve filters and existing rows where safe; offer retry and safe trace reference.
- Updating row: keep position stable and disable only the affected intent.
- Long content: wrap or deliberately truncate the primary identifier with full accessible recovery.
- Large datasets: use server pagination; do not render all records to mimic smooth scrolling.

## Accessibility

Use semantic table markup for tabular data, proper header associations, accessible sort state, labeled selection checkboxes, keyboard-operable menus, visible focus, and non-color-only status. Announce selection counts and mutation results without moving focus unpredictably.
