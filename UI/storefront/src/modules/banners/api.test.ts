import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { getPublicBannerSection, publicBannerSectionUrl } from "./api";
import { BANNER_SECTION_KEYS } from "./section-config";

describe("storefront banner API istemcisi", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  // Burada storefront'un altı bölümü yalnız anonim public endpointlerinden okuduğunu doğruluyorum.
  it.each(BANNER_SECTION_KEYS)("%s için doğru public GET yolunu kullanır", async (key) => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      name: key,
      key,
      items: [],
    }), {
      status: 200,
      headers: { "content-type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    await getPublicBannerSection(key);

    const expectedPath = key === "main-banner" ? "/api/main-banners" : `/api/${key}`;
    expect(new URL(fetchMock.mock.calls[0][0]).pathname).toBe(expectedPath);
    expect(fetchMock.mock.calls[0][1]).not.toHaveProperty("headers.Authorization");
  });

  // Burada URL birleştirmesinin API origin'i ve bölüm yolunu doğru koruduğunu doğruluyorum.
  it("public URL'yi verilen API origin'inde üretir", () => {
    expect(publicBannerSectionUrl("alt-banner-5", "https://api.example.test/root")).toBe(
      "https://api.example.test/api/alt-banner-5",
    );
  });
});
