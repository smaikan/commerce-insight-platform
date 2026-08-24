import { describe, expect, it } from "vitest";

import {
  safeGoogleMapsEmbedUrl,
  safeStoreSettingsUrl,
  storeMapNavigationUrl,
} from "@/modules/store-settings/url";

describe("store settings URLs", () => {
  it("accepts only absolute HTTP or HTTPS settings URLs", () => {
    expect(safeStoreSettingsUrl("https://example.com/path")).toBe("https://example.com/path");
    expect(safeStoreSettingsUrl("javascript:alert(1)")).toBeNull();
    expect(safeStoreSettingsUrl("not-a-url")).toBeNull();
  });

  it("recognizes a Google Maps embed URL without trusting lookalike hosts", () => {
    const embedUrl = "https://www.google.com/maps/embed?pb=store-location";

    expect(safeGoogleMapsEmbedUrl(embedUrl)).toBe(embedUrl);
    expect(safeGoogleMapsEmbedUrl("https://example.com/google.com/maps/embed?pb=fake")).toBeNull();
    expect(safeGoogleMapsEmbedUrl("http://www.google.com/maps/embed?pb=insecure")).toBeNull();
  });

  it("turns an embed-only URL into a normal Google Maps navigation link", () => {
    expect(storeMapNavigationUrl(
      "https://www.google.com/maps/embed?pb=store-location",
      "Altıyol Meydanı, Kadıköy/İstanbul",
    )).toBe("https://www.google.com/maps/search/?api=1&query=Alt%C4%B1yol+Meydan%C4%B1%2C+Kad%C4%B1k%C3%B6y%2F%C4%B0stanbul");
  });

  it("preserves a non-embed map provider URL for normal navigation", () => {
    expect(storeMapNavigationUrl("https://maps.example.com/store", "İstanbul"))
      .toBe("https://maps.example.com/store");
  });
});
