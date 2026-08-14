import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { catalogSegmentFromApiUrl, classificationSegmentFromName } from "./classification-url";

describe("catalog classification", () => {
  // Burada API'de URL alanı olmayan Türkçe ürün türü adlarının örnek category rotasıyla aynı kararlı segmente dönüştüğünü doğruluyorum.
  it("creates stable ASCII category segments from Turkish names", () => {
    expect(classificationSegmentFromName("Yüzük")).toBe("yuzuk");
    expect(classificationSegmentFromName("  Kadın & Çocuk Ürünleri  ")).toBe("kadin-cocuk-urunleri");
  });

  // Burada teknik URL'deki büyük ASCII I harfinin Türkçe dotless-ı dönüşümüne uğramadan kullanıcı örneğindeki lowercase adresi verdiğini doğruluyorum.
  it("normalizes API URL casing without locale-sensitive mutations", () => {
    expect(catalogSegmentFromApiUrl("SERANTIS")).toBe("serantis");
  });
});
