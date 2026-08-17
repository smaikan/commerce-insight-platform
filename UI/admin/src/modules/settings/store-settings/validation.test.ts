import { describe, expect, it } from "vitest";
import { parseStoreSettingsCommit } from "./validation";

const token = "8f9b61ec-37d5-4ca2-a6ef-5dc820d4151d";

describe("store settings validation", () => {
  it("accepts the deterministic .NET Guid seed concurrency token", () => {
    const result = parseStoreSettingsCommit({
      section: "identity",
      expectedConcurrencyToken: "22222222-2222-2222-2222-222222222222",
      values: {
        displayName: "Ayda Home",
        shortDescription: null,
        logoUrl: null,
        darkLogoUrl: null,
        faviconUrl: null,
        defaultShareImageUrl: null,
      },
    });

    expect(result.ok).toBe(true);
  });

  it("normalizes identity values without inventing fields", () => {
    const result = parseStoreSettingsCommit({
      section: "identity",
      expectedConcurrencyToken: token,
      values: {
        displayName: "  Ayda Home  ",
        shortDescription: "   ",
        logoUrl: "https://res.cloudinary.com/demo/image/upload/store-settings/logo/a.png",
        darkLogoUrl: null,
        faviconUrl: null,
        defaultShareImageUrl: null,
        ignored: "not-sent",
      },
    });

    expect(result).toEqual({
      ok: true,
      value: {
        section: "identity",
        expectedConcurrencyToken: token,
        values: {
          displayName: "Ayda Home",
          shortDescription: null,
          logoUrl: "https://res.cloudinary.com/demo/image/upload/store-settings/logo/a.png",
          darkLogoUrl: null,
          faviconUrl: null,
          defaultShareImageUrl: null,
        },
      },
    });
  });

  it("rejects stale-shaped commits without a valid concurrency token", () => {
    const result = parseStoreSettingsCommit({ section: "legal", expectedConcurrencyToken: "old", values: {} });
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors.expectedConcurrencyToken).toBeDefined();
  });

  it("requires one exact title placeholder and absolute social URLs", () => {
    const result = parseStoreSettingsCommit({
      section: "seo",
      expectedConcurrencyToken: token,
      values: {
        defaultTitle: "Mağaza",
        titleTemplate: "%s | %s",
        defaultDescription: null,
        defaultOpenGraphImageUrl: null,
        allowIndexing: true,
        facebookUrl: "/facebook",
        instagramUrl: null,
        tiktokUrl: null,
        youtubeUrl: null,
        xUrl: null,
        pinterestUrl: null,
      },
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.fieldErrors.titleTemplate).toBeDefined();
      expect(result.fieldErrors.facebookUrl).toBeDefined();
    }
  });

  it("accepts the numeric storefront enum contract", () => {
    const result = parseStoreSettingsCommit({
      section: "storefront",
      expectedConcurrencyToken: token,
      values: {
        status: 1,
        statusMessage: "Kısa süre sonra yeniden buradayız.",
        showOutOfStockProducts: false,
        showProductsWithoutPrice: false,
        defaultProductSort: 2,
        defaultProductSortDescending: false,
        showCompareAtPrice: true,
        showStockWarning: true,
        lowStockThreshold: 5,
      },
    });

    expect(result.ok).toBe(true);
  });

  it("enforces the documented low-stock range", () => {
    const result = parseStoreSettingsCommit({
      section: "storefront",
      expectedConcurrencyToken: token,
      values: {
        status: 0,
        statusMessage: null,
        showOutOfStockProducts: true,
        showProductsWithoutPrice: true,
        defaultProductSort: 0,
        defaultProductSortDescending: true,
        showCompareAtPrice: true,
        showStockWarning: true,
        lowStockThreshold: 0,
      },
    });

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.fieldErrors.lowStockThreshold).toBeDefined();
  });
});
