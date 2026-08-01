# E-Commerce Structured Data

## Safe rendering

Render server-side:

```tsx
<script
  type="application/ld+json"
  dangerouslySetInnerHTML={{
    __html: JSON.stringify(data).replace(/</g, "\\u003c"),
  }}
/>
```

Never insert prebuilt user-controlled JSON strings.

## Product

Use `Product` for a single product or directly selected variant. Verify:

- `@context: "https://schema.org"` and `@type: "Product"`.
- `name`, canonical `url`, image, description, and stable `sku`/supported identifier.
- `brand` only when known.
- `offers` with a real absolute URL, price, three-letter currency, schema.org availability, and condition.
- Rating/review only from approved, visible data and with the required counts/values.
- JSON-LD price, stock, name, image, and selected variant match the visible page.

Do not include invented GTIN, MPN, price-valid-until, shipping, returns, reviews, or ratings.

## ProductGroup and variants

Use `ProductGroup` only for a genuine variant family.

- Give the group a stable `productGroupID`, name, and canonical base product URL.
- Set `variesBy` using supported full schema.org properties such as color, size, material, pattern, age, or gender.
- Represent each variant as a complete `Product` with a unique ID/SKU and accurate offer.
- Use `hasVariant` for nested variants or `isVariantOf`/`inProductGroupWithID` consistently.
- Make every selectable variant directly addressable so its URL restores the correct image, price, availability, and cart selection.
- For a single-page variant model, keep one canonical group URL unless the product strategy intentionally uses equally important variant pages.
- Keep markup in initial HTML for reliable shopping crawls.

If the API cannot express what variants vary by, report the gap; do not infer color/size from free text.

## BreadcrumbList

- Match the visible hierarchy, not the filesystem.
- Use ordered `ListItem` entries with consecutive positions starting at 1.
- Use absolute canonical URLs.
- Keep names consistent with visible breadcrumbs.
- Include home, useful category/collection hierarchy, and current page as appropriate.
- Do not invent a category relationship absent from product/catalog data.

## Validation

- Validate syntax and schema.org shape.
- Run Google Rich Results Test for Google eligibility.
- Check rendered initial HTML and current API truth.
- Re-test after deployment because templates, caching, and environment origins can change output.
