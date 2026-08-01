# Forms, Detail Screens, and Overlays

## Complex form layout

At wide desktop sizes use:

- primary column for identity/content and repeated line/item work;
- secondary rail for status, activation, organization, relationships, and concise summaries;
- compact page header and explicit save/overflow actions.

Stack the rail after the primary decision-critical fields on smaller screens. Do not turn every field group into a heavy card; use surfaces only for meaningful grouping.

## Product and variant workflow

Adapt the local product reference into supported groups:

- product identity: title, main SKU, URL, description;
- media: image URL, alternative text, main image, order;
- organization: brand, product type, collections, tags, tax rate;
- state: product status, active, featured, display order;
- variants: name/value, SKU, price, compare-at price, barcode, material, activation;
- SEO: title and description with a realistic search-preview treatment.

Respect API boundaries:

- Product creation may atomically include variants.
- Product update, relationships, images, variant updates, price, activation, and stock movement are distinct operations.
- Do not present a false all-or-nothing save when several endpoints can partially succeed; sequence transparently and report partial failure.
- `hasVariants` and net price are response-only/derived.
- Never edit stock as a plain product field. Create a signed StockMovement with type and reason.
- Omit unsupported publishing channels, vendor, shipping weight, theme template, and Shopify-specific metadata.

## Order, return, customer, and accounting details

- Order: present immutable snapshot data, totals, items, payments, address, reservation, timestamps, and only valid lifecycle actions.
- Return: show requested items, notes, refund total, lifecycle timestamps, and the next allowed approve/reject/receive/complete action.
- Customer: show profile/session facts and bound role/status actions; handle last-admin conflicts.
- Accounting: separate e-commerce orders from accounting sales orders; display Draft/Posted/Cancelled rules, reversals, idempotency, and authoritative balances exactly as documented.

Do not create a timeline event unless the API returns a corresponding state/timestamp. Re-read detail after every lifecycle mutation.

## Validation and save behavior

- Use persistent labels and appropriate input types.
- Map ProblemDetails field errors to controls and show a summary for long forms.
- Preserve safe input on validation, business rule, conflict, and network errors.
- Track dirty state by meaningful form values, not focus history.
- Prevent duplicate submission and communicate saving/saved/failed states without layout movement.
- On concurrency conflict, fetch the current record, show what changed, and require a new user decision.
- Keep the same idempotency key when retrying the same supported operation.

## Dialog, drawer, and confirmation rules

Use a dialog for a bounded decision or compact form that must block the background. Use a drawer for contextual preview or a short secondary task while preserving list context. Use a full page for complex/repeatable fields, media, variants, orders, or accounting documents.

Every overlay must provide:

- an accessible name and optional description;
- deliberate initial focus;
- focus containment and restoration;
- keyboard close when safe;
- visible primary/secondary hierarchy;
- pending, error, and disabled behavior;
- responsive bounds without off-screen actions.

Confirmations must state the resource, action, consequence, and reversibility. Require typed confirmation only for unusually destructive, broad, or irreversible operations; do not add friction to ordinary reversible edits.
