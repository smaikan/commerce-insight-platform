# Indexability and Metadata

## Route policy

| Route family | Default index policy | Canonical | Sitemap |
| --- | --- | --- | --- |
| Home | index | self | yes |
| Product | index when active/public | current product URL | yes |
| Category/collection | index when useful and populated | self | yes |
| Curated filter landing | index only with unique intent/content | self | yes |
| Arbitrary filters/facets | noindex, follow | usually clean category or self by intent | no |
| Sort/view/tracking params | avoid duplicate indexing | clean equivalent URL | no |
| Internal search | noindex, follow | normally self or omitted; do not misuse canonical as noindex | no |
| Pagination | usually indexable/addressable | self, not page 1 | optional by strategy |
| Login/account/cart/checkout | noindex | self or omitted | no |
| Admin/internal API | noindex and authenticated | none | no |

Do not combine contradictory signals casually. Canonical consolidates duplicates; `noindex` removes a page from search. Choose based on intent.

## Metadata review

- Set `metadataBase` from the production site origin.
- Use a root title template and route-specific titles.
- Keep one clear page topic across title, description, `h1`, canonical, and Open Graph.
- Generate product metadata from the same authoritative fetch used by the page.
- Return `notFound()` for nonexistent public products and prevent inactive/private products from being indexable.
- Ensure canonical generation removes tracking, sort, view, and non-canonical filter parameters.
- Never point canonical to a URL with materially different products/content.
- Use absolute Open Graph image URLs with accurate alt text.
- Confirm inherited metadata does not leave all dynamic pages with the same canonical.

## Robots and noindex

- `robots.ts` controls crawling; route metadata or `X-Robots-Tag` controls indexing.
- A crawler blocked by robots may not see `noindex`.
- Protect confidential content with authentication, not robots/noindex.
- Avoid disallowing assets needed to render public pages.
- Verify production robots output, not only source code.

## Sitemap

- Include canonical, indexable, successful URLs only.
- Exclude parameter duplicates and private routes.
- Use real modification timestamps.
- Ensure route generation can enumerate every intended product/category.
- Validate XML, origin, redirects, status codes, and sitemap index splitting when large.
