import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import type { StorefrontNavigationGroup } from "./navigation-types";
import { DesktopNavigation } from "./desktop-navigation";

vi.mock("next/navigation", () => ({ usePathname: () => "/products" }));

const groups: StorefrontNavigationGroup[] = [
  {
    id: "categories",
    label: "Kategoriler",
    items: [{ id: "type-1", label: "Yüzük", href: "/category/yuzuk", productCount: 8 }],
  },
  {
    id: "collections",
    label: "Koleksiyonlar",
    href: "/collections",
    items: [{ id: "collection-1", label: "Takı", href: "/collection/taki", productCount: 5 }],
  },
  {
    id: "brands",
    label: "Markalar",
    items: [{ id: "brand-1", label: "SERANTIS", href: "/brand/serantis", productCount: 6 }],
  },
];

describe("desktop navigation", () => {
  // Burada masaüstü navigasyonunun ana hedefleri, aktif sayfayı ve yalnızca dolu açılır menü tetikleyicilerini erişilebilir biçimde sunduğunu doğruluyorum.
  it("renders direct links and disclosure triggers", () => {
    const html = renderToStaticMarkup(<DesktopNavigation groups={groups} />);

    expect(html).toContain('aria-label="Ana navigasyon"');
    expect(html).toContain('href="/"');
    expect(html).toContain('href="/products"');
    expect(html).toContain('aria-current="page"');
    expect(html).toContain("Kategoriler");
    expect(html).toContain("Koleksiyonlar");
    expect(html).toContain('href="/collections"');
    expect(html).toContain('aria-label="Koleksiyonlar alt menüsünü aç"');
    expect(html).toContain("Markalar");
    expect(html.match(/aria-expanded="false"/g)).toHaveLength(3);
  });
});
