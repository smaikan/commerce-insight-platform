import { describe, expect, it, vi } from "vitest";

// Burada metadata testini ortam değişkenlerinden bağımsız, sabit bir admin origin ve adla çalıştırıyorum.
vi.mock("@/lib/site-config", () => ({
  siteConfig: {
    name: "Mağaza",
    url: "http://localhost:3001",
  },
}));

import { buildAdminRootMetadata } from "./metadata";

describe("admin store settings metadata", () => {
  // Burada kaydedilen güvenli favicon adresinin admin metadata'sına aktarıldığını doğruluyorum.
  it("uses the configured StoreSettings favicon", () => {
    const metadata = buildAdminRootMetadata("https://res.cloudinary.com/demo/image/upload/store-settings/favicon/admin.webp");

    expect(metadata.icons).toEqual({
      icon: "https://res.cloudinary.com/demo/image/upload/store-settings/favicon/admin.webp",
    });
  });

  // Burada tehlikeli veya bozuk favicon adreslerinin admin belge başlığına taşınmadığını doğruluyorum.
  it.each(["javascript:alert(1)", "not-a-url"])("rejects unsafe favicon URL %s", (faviconUrl) => {
    expect(buildAdminRootMetadata(faviconUrl).icons).toBeUndefined();
  });

  // Burada dinamik favicon eklenirken admin noindex politikasının değişmediğini doğruluyorum.
  it("preserves the admin robots policy", () => {
    expect(buildAdminRootMetadata(null).robots).toEqual({
      index: false,
      follow: false,
      nocache: true,
    });
  });
});
