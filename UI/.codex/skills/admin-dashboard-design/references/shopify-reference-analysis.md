# Local Shopify Reference Analysis

## Sources inspected

Located under `docs/references/admin-ui/shopify`:

- `Ürünler listesi.png`
- `Koleksiyonlar listesi.png`
- `koleksiyon sayfası.png`
- `indirimler.png`
- `Ürün sayfası.pdf` (three rendered pages)

Treat these files as visual references only. Do not copy Shopify logos, names, store identity, proprietary icons, exact assets, or unsupported product concepts.

## Patterns worth adapting

- A stable neutral sidebar separates global navigation from the work surface.
- A compact topbar reserves space for search/navigation commands and account utilities.
- Page headers keep title/context left and a small action cluster right.
- List pages put tabs/view controls, search/filter, columns, and the table inside one coherent surface.
- Rows are dense but legible, with thumbnails, status labels, aligned values, and restrained separators.
- Nested navigation shows parent context without deep accordion complexity.
- Complex edit pages use a broad primary column and narrower context/status rail.
- Forms group fields by workflow: identity/content, media, price, inventory, variants, organization, and SEO.
- Detail summaries expose current status and rules without repeating the entire form.
- Overlays and controls use modest radius, borders, and minimal shadow.

## Adapt for this project

- Replace the black/Shopify-branded header with this product's neutral shell and controlled blue accents.
- Use blue for primary action, active navigation cue, link, selection, and focus—not every surface.
- Translate the density and hierarchy, not the exact pixel geometry.
- Keep Turkish operator copy but English URL segments and code identifiers.
- Base sidebar groups on this API's catalog, orders, returns, inventory, users, coupons, shipping/tax, and accounting modules.
- Use the project's status enums, lifecycle rules, public IDs, filters, pagination, conflicts, and errors.
- Use local shared icons consistently; do not imitate Shopify glyphs.

## Do not copy

- Sales channels, Markets, Content, Growth, apps, theme templates, publishing controls, shipping weight, vendor concepts, or global search unless this API actually supports them.
- Bulk controls when the operation is not documented.
- Metric cards, trends, or charts without authoritative data.
- A card around every section merely because the reference uses a surface.
- Shopify's brand colors, logo, store badges, account content, product data, or wording.

## Reference interpretation

Use the references primarily for:

1. shell proportions and navigation clarity;
2. operational density;
3. list/filter/table composition;
4. two-column form hierarchy;
5. placement of primary versus secondary actions;
6. restrained visual effects.

When the reference conflicts with accessibility, real data, mobile behavior, API capability, or the shared design system, document the deviation and follow the project requirement.
