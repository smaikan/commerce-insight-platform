# Responsive Layout and Real States

## Viewport review

Review at representative narrow mobile, wider mobile, tablet when layout changes, and desktop sizes. Include a dense or large desktop only when it changes the task layout.

At each width inspect:

- container padding and readable line length;
- grid collapse and source order;
- header/navigation, filters, drawers, and sticky regions;
- image aspect ratio and crop;
- text wrapping, truncation, and action labels;
- control hit areas and spacing;
- modal/drawer bounds and virtual-keyboard obstruction;
- horizontal overflow, clipping, collision, and unreachable actions;
- persistent totals and primary actions without hiding content.

Do not preserve desktop composition by merely shrinking text. Recompose priority and order for mobile while keeping the same task and information.

## Commerce stress fixtures

Use API constraints when documented. Otherwise choose plausible extremes and label them as test fixtures.

Test:

- short and very long product/brand/variant names;
- one and many variants;
- missing primary image and mixed image ratios;
- low, high, discounted, and price-changed values using locale formatting;
- available, low-stock, unavailable, and out-of-stock items;
- long descriptions and no description;
- empty, single-item, and dense product grids/carts;
- long validation and server error messages;
- loading, partial data, timeout, and retry;
- long translated labels and user-provided address/name content.

Never solve long content by hiding information required for purchase. Use wrapping, deliberate truncation with recovery, layout reflow, or a denser component variant.

## Component density

- Product grid: preserve scanability, stable media, title/price/stock priority, and comparable card heights without large blank areas.
- Product detail: prioritize media, title, price, variant/stock, and purchase action before promotional content.
- Cart/checkout: keep item identity, editable values, validation, totals, and primary action easy to associate.
- Account/admin: prefer compact controls, tables/lists, and visible state over large marketing cards.
- Touch layouts: preserve target size without turning every control into an oversized pill.

## Professional states

### Loading

Reserve final geometry, show only useful skeleton groups, and prevent layout shift. Prefer a compact progress indicator when the final shape is unknown.

### Empty

Name the absent content, explain only what helps, and offer one real next step. Do not add fake metrics, large illustrations, or multiple decorative cards by default.

### Error

State what failed in user terms, retain valid content/input, show a trace/reference only when useful, and offer retry/back/support actions that truly work.

### Disabled

Keep text readable and semantics correct. If the reason is not obvious, place concise explanatory text nearby. Do not rely on opacity alone or use disabled styling for a loading action without a busy state.

### Missing/unavailable

Keep image placeholders stable and branded but quiet. Make stock and price changes explicit. Disable purchase only when required and give the next valid action.
