import { describe, expect, it } from "vitest";
import { navigationSections, navigationStatusLabel } from "./navigation";

describe("admin navigation", () => {
  it("keeps implemented phase-one routes enabled", () => {
    const enabledItems = navigationSections
      .flatMap((section) => section.items)
      .filter((item) => item.href);

    expect(enabledItems).toEqual([
      { label: "Dashboard", href: "/dashboard", status: "available" },
      { label: "Siparişler", href: "/orders", status: "available" },
      { label: "Ürünler", href: "/products", status: "available" },
    ]);
  });

  it("labels unavailable navigation items without inventing routes", () => {
    const unavailableItems = navigationSections
      .flatMap((section) => section.items)
      .filter((item) => item.status !== "available");

    expect(unavailableItems.every((item) => item.href === undefined)).toBe(true);
    expect(unavailableItems.some((item) => item.label === "Ürün Ekle")).toBe(false);
    expect(navigationStatusLabel("next")).toBe("Sırada");
    expect(navigationStatusLabel("planned")).toBe("Planlı");
    expect(navigationStatusLabel("future")).toBe("Yakında");
  });
});
