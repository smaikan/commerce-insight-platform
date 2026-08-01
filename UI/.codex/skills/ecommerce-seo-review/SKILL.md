---
name: ecommerce-seo-review
description: Audit, review, plan, or fix SEO in a Next.js App Router e-commerce application. Use for generateMetadata, titles and descriptions, canonical URLs, Open Graph, sitemap.ts, robots.ts, JSON-LD, Product/ProductGroup/BreadcrumbList structured data, category/filter/search indexability decisions, crawl control, and Core Web Vitals risks or measurements.
---

# E-Commerce SEO Review

Review SEO as a route and rendering system, not as a checklist of tags. Separate verified defects from recommendations and from items that require production field data.

## Start with evidence

1. Locate the nearest Next.js project root and inspect `package.json`, Next config, `src/app` or `app`, environment examples, and route structure.
2. Run the static inventory:

   ```powershell
   node .codex/skills/ecommerce-seo-review/scripts/seo-static-audit.mjs .
   ```

3. Read [indexability-and-metadata.md](references/indexability-and-metadata.md) for route decisions.
4. Read [structured-data.md](references/structured-data.md) when reviewing product, variant, category, or breadcrumb markup.
5. Read [core-web-vitals.md](references/core-web-vitals.md) when reviewing performance or making CWV claims.
6. Verify framework behavior against the current official Next.js documentation and search eligibility against current Google Search Central guidance. Use schema.org for vocabulary, not as proof of Google rich-result eligibility.
7. Inspect rendered HTML or a production-like build when runtime behavior matters. Static source presence alone does not prove correct output.

Treat script findings as leads. Confirm every reported issue in the owning route, inherited layout metadata, rendered HTML, or runtime headers before presenting it as a defect.

## Use available MCP tools

- Use **Next DevTools MCP** (`next_devtools`) to inspect App Router routes/runtime behavior and retrieve version-matched Next.js metadata, sitemap, robots, and rendering guidance.
- Use **Chrome DevTools MCP** (`chrome_devtools`) to inspect the rendered document, response/redirect chain, canonical and robots output, JSON-LD, console/network failures, device rendering, and Lighthouse SEO diagnostics.
- Use **Playwright MCP** (`playwright`) for repeatable route-family, device, missing-product, redirect, and authenticated/noindex checks. Keep durable regressions in the repository's test suite rather than only in an MCP session.
- Do not treat a browser DOM, Lighthouse score, or Playwright pass as proof of Google indexing or rich-result eligibility. If an MCP is unavailable, use equivalent production HTML/CLI inspection and mark the missing evidence.

## Build a route indexability matrix

Classify every route family as:

- indexable and self-canonical;
- indexable but canonicalized to an equivalent preferred URL;
- crawlable with `noindex`;
- private/authenticated and `noindex`;
- intentionally crawl-blocked for crawl-budget control.

Record the reason, canonical target, sitemap inclusion, robots behavior, and structured-data type for each family. Do not use one global rule for all query parameters.

Apply these defaults unless product intent or content quality justifies another choice:

- Index home, useful category/collection landing pages, and canonical product pages.
- Use `noindex, follow` for internal search and low-value arbitrary filter combinations.
- Use self-canonical URLs for genuinely distinct, useful landing pages.
- Canonicalize tracking and duplicate sort/view parameters to the clean equivalent URL.
- Keep paginated pages individually addressable; do not canonicalize every page to page 1.
- Use `noindex` for login, account, cart, checkout, admin, and internal operations.
- Do not block a page in `robots.ts` when relying on its `noindex` metadata; crawlers must fetch it to see the rule.

## Review metadata

Check:

- Root `metadataBase`, title template, default description, and site-wide Open Graph defaults.
- Static `metadata` for stable routes and `generateMetadata` for route/data-dependent values.
- Unique, descriptive title and description aligned with the visible `h1` and page intent.
- Absolute, normalized, HTTPS canonical URLs without tracking parameters.
- Self-canonical metadata on canonical pages and valid targets on duplicates.
- Open Graph title, description, canonical URL, type, locale, site name, image, dimensions, and alt text.
- Metadata behavior for missing, inactive, or non-indexable products.
- Shared data-fetch deduplication between `generateMetadata` and the page.

Do not fail solely on a character-count heuristic. Flag truncation, duplication, keyword stuffing, missing intent, or misleading copy instead.

## Review sitemap and robots

Check `src/app/sitemap.ts` and `src/app/robots.ts`:

- Emit only absolute canonical URLs that are indexable and expected to return `200`.
- Exclude search, filters not selected as landing pages, auth, account, cart, checkout, admin, and redirects.
- Source product/category URLs from authoritative data rather than hard-coded samples.
- Use truthful `lastModified`; do not set every URL to the current build time without evidence.
- Split large sitemaps when platform/search-engine limits require it.
- Reference the sitemap from robots and use the production origin.
- Keep robots crawl rules separate from page-level indexability decisions.
- Avoid disallowing CSS, JavaScript, images, or data required to render indexable pages.

## Review structured data

- Render JSON-LD in the initial server HTML on the page it describes.
- Make structured data match visible, current API data exactly.
- Escape `<` in serialized JSON to prevent script injection.
- Use `Product` for a single purchasable product/variant.
- Use `ProductGroup` plus nested or linked `Product` variants only when the page truly represents variants.
- Use `BreadcrumbList` that matches visible hierarchy and canonical URLs.
- Give products/variants stable unique identifiers and absolute URLs.
- Include offers only for real purchasable variants; map price, currency, availability, and condition accurately.
- Never fabricate ratings, reviews, GTIN, discounts, shipping, return policy, or stock.
- Validate eligible markup with Google Rich Results Test and syntax/schema coverage separately.

Do not add product rich-result markup to generic category/list/search pages.

## Review Core Web Vitals

Assess code risks for LCP, INP, and CLS, but do not claim a pass without field data.

- Prefer Server Components and minimize hydration/client bundles.
- Optimize the actual LCP asset; use `next/image`, correct responsive sizes, stable dimensions, and eager/high-priority loading only for the real above-the-fold candidate.
- Use `next/font` or otherwise prevent font-driven layout shifts.
- Reserve space for images, banners, consent UI, recommendations, and async content.
- Reduce long client tasks, broad contexts, unnecessary effects, and third-party scripts.
- Use appropriate Script strategies and lazy-load non-critical widgets.
- Stream useful server-rendered content and avoid client waterfalls.
- Inspect production bundle and network behavior before recommending dependencies or caching changes.
- Prefer CrUX, Search Console, or RUM at the 75th percentile; use Lighthouse as a lab diagnostic.

## Report findings

Order findings by:

1. Critical: accidental deindexing, wrong canonical domain/target, private URLs indexable, invalid product truth, or severe crawl traps.
2. High: missing unique metadata on important routes, broken sitemap/robots interaction, absent/invalid eligible structured data, or likely major CWV regression.
3. Medium: incomplete Open Graph, weak snippets, inefficient crawl patterns, or measurable performance opportunities.
4. Low: polish and optional enhancements.

For every finding provide:

- severity and rule;
- route family;
- file and line evidence when available;
- rendered/runtime evidence when checked;
- impact;
- minimal fix;
- validation method.

State “not verified” when production HTML, Search Console, CrUX/RUM, or deployment headers are unavailable. Do not convert speculative SEO advice into a guaranteed ranking claim.

When asked only to review, do not edit files. When asked to fix, make the smallest changes, preserve existing architecture, run the static audit again, build the application, and verify affected rendered pages.
