# Next.js Runtime Optimization

## Server/client boundaries

- Server Components add no component JavaScript to the client by default.
- A Client Component boundary pulls its imports and descendants into the client graph.
- Keep static wrappers, headings, tables, product copy, and initial fetching server-side.
- Extract buttons, dialogs, field arrays, menus, and browser-only APIs into leaf clients.
- `next/image` can be imported by a Server Component; its internal implementation does not require the caller to add `"use client"`.

## Bundle investigation

- Check the installed Next CLI before using analyzer commands.
- Prefer the built-in analyzer when supported; inspect route-specific client and server graphs.
- Trace large modules to the first server-to-client boundary.
- Prefer direct imports when a package's barrel prevents tree-shaking.
- Dynamically import non-critical editors, charts, maps, reviews, and support widgets.
- Do not dynamically import core product content or the true LCP element.
- Verify bundle reduction in a production build.

## Render and interaction

- Avoid client fetch-on-mount for initial page data.
- Avoid broad contexts whose value changes frequently.
- Keep state closest to the interaction.
- Defer non-urgent updates when appropriate, but fix expensive work first.
- Virtualize only genuinely large interactive lists; preserve crawlable server-rendered catalog paths.
- Profile slow handlers before adding memoization.

## Third parties

- Inventory owner, route, strategy, transfer, CPU, and business value.
- Load only on routes that need the integration.
- Delay nonessential scripts until after interaction/idle where acceptable.
- Remove duplicate analytics and tag-manager injections.
- Treat consent and payment scripts as correctness-sensitive; validate flows after changes.
