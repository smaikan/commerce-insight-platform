# Admin Shell and Navigation

## Suggested route and navigation map

Use only modules currently implemented and authorized:

```text
/admin
/admin/products
/admin/products/[productId]
/admin/collections
/admin/brands
/admin/product-types
/admin/tags
/admin/orders
/admin/orders/[orderId]
/admin/returns
/admin/inventory/stock-movements
/admin/customers
/admin/coupons
/admin/shipping-methods
/admin/tax-rates
/admin/accounting/**
```

Keep e-commerce `orders` separate from accounting `sales-orders`.

## Sidebar structure

Group by operator task rather than controller count:

- Overview
- Catalog: products, collections, brands, product types, tags
- Sales: orders, returns
- Inventory: stock movements
- Customers
- Promotions: coupons
- Configuration: shipping methods, tax rates
- Accounting: current accounts, sales/purchases, payments, treasury, costing, reports

Render only groups the current admin can access. Do not treat a hidden link as authorization.

Keep one expanded subtree based on the current route when possible. Use a restrained active row, clear parent state, consistent 16–20 px icons, and labels that do not truncate at ordinary Turkish lengths. Put rare configuration near the bottom without inventing a generic settings page.

## Topbar

Use the topbar for:

- mobile sidebar trigger;
- breadcrumb or compact current context when helpful;
- navigation command/search only when honest about its scope;
- notifications only when real notification data exists;
- account/session menu.

Do not place several global CTAs in the topbar. Keep page-level actions in the page header.

## Page frame

- Let list/report screens use broad available width.
- Bound long forms to a readable working width while keeping the secondary rail nearby.
- Keep a compact page header with breadcrumb/back context, title/status, and primary/overflow actions.
- Avoid nested page-background cards. A table or meaningful form group may provide the main surface.
- Use sticky page/save actions only when they remain reachable, do not cover content, and reflect dirty/submitting state.

## Responsive behavior

- Desktop: persistent sidebar and full table/form composition.
- Intermediate width: collapse lower-priority columns, tighten gaps, and allow the form rail to narrow or stack.
- Tablet/mobile: move sidebar into an accessible drawer, keep a compact topbar, stack detail rails in task order, and make action bars wrap or dock safely.
- Preserve table access with horizontal scrolling or an explicit priority list; keep identifiers/status/actions recoverable.
- Restore focus after closing navigation or overlays and prevent background interaction while modal UI is open.

Desktop-first means optimized for repeated keyboard/mouse operations, not unusable on mobile.

## Suggested ownership

```text
src/app/(admin)/admin/layout.tsx
src/modules/admin-shell/
  components/admin-sidebar.tsx
  components/admin-topbar.tsx
  components/page-header.tsx
  navigation.ts
```

Keep feature pages thin. Place catalog, order, inventory, customer, return, coupon, and accounting UI in their owning modules. Keep client state at the smallest interactive leaf.
