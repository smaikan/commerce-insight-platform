import { describe, expect, it } from "vitest";

import { buildRootMetadata } from "@/modules/store-settings/metadata";

describe("store settings metadata", () => {
  // Burada yayınlanan güvenli favicon adresinin Next.js metadata sözleşmesine aktarıldığını doğruluyorum.
  it("uses the configured public favicon", () => {
    const metadata = buildRootMetadata({
      displayName: "ELEVEN",
      faviconUrl: "https://res.cloudinary.com/demo/image/upload/favicon.webp",
    });

    expect(metadata.icons).toEqual({
      icon: "https://res.cloudinary.com/demo/image/upload/favicon.webp",
    });
  });

  // Burada mağaza adının ana sekme başlığına ve alt sayfa başlık şablonuna uygulandığını doğruluyorum.
  it("uses the public store name in browser titles", () => {
    const metadata = buildRootMetadata({ displayName: "ELEVEN", faviconUrl: null });

    expect(metadata.title).toEqual({
      default: "ELEVEN",
      template: "%s | ELEVEN",
    });
    expect(metadata.openGraph).toMatchObject({
      siteName: "ELEVEN",
      title: "ELEVEN",
    });
  });

  // Burada geçersiz protokollü favicon değerinin belge başlığına taşınmadığını doğruluyorum.
  it("rejects an unsafe favicon URL", () => {
    const metadata = buildRootMetadata({ displayName: "ELEVEN", faviconUrl: "javascript:alert(1)" });

    expect(metadata.icons).toBeUndefined();
  });
});
