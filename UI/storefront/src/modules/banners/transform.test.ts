import { describe, expect, it } from "vitest";

import { normalizePublicBannerSection } from "./transform";
import type { BannerSection, BannerSectionItem } from "./types";

function item(overrides: Partial<BannerSectionItem> = {}): BannerSectionItem {
  return {
    id: crypto.randomUUID(),
    name: "Banner",
    key: "banner",
    mediaUrl: "https://res.cloudinary.com/demo/image/upload/banner.jpg",
    mediaType: 1,
    displayOrder: 0,
    isActive: true,
    isMain: false,
    ...overrides,
  };
}

function section(items: BannerSectionItem[]): BannerSection {
  return { name: "Banner alanı", key: "section", items };
}

describe("public banner dönüşümü", () => {
  it("ana bannerı öne alır, pasifi çıkarır ve sonucu beş öğeyle sınırlar", () => {
    const result = normalizePublicBannerSection("main-banner", section([
      item({ key: "passive", isActive: false, displayOrder: 0 }),
      item({ key: "one", displayOrder: 1 }),
      item({ key: "two", displayOrder: 2 }),
      item({ key: "three", displayOrder: 3 }),
      item({ key: "four", displayOrder: 4 }),
      item({ key: "five", displayOrder: 5 }),
      item({ key: "main", displayOrder: 99, isMain: true }),
    ]));

    expect(result.items).toHaveLength(5);
    expect(result.items.map((entry) => entry.key)).toEqual(["main", "one", "two", "three", "four"]);
  });

  it("alt banner kayıtlarında isMain değerini kapatıp displayOrder ile sıralar", () => {
    const result = normalizePublicBannerSection("alt-banner-1", section([
      item({ key: "later", displayOrder: 2, isMain: true }),
      item({ key: "first", displayOrder: 0 }),
    ]));

    expect(result.items.map((entry) => entry.key)).toEqual(["first", "later"]);
    expect(result.items.every((entry) => entry.isMain === false)).toBe(true);
  });
});
