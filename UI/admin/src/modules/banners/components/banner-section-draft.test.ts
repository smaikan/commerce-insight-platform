import { describe, expect, it } from "vitest";
import {
  createBannerItemDraft,
  moveBannerDraftItem,
  removeBannerItem,
  selectMainBanner,
  suggestBannerKey,
  toBannerCommitItems,
} from "./banner-section-draft";

describe("banner section draft", () => {
  // Burada anahtar önerisinin mevcut kullanıcı değerini yönetmeden yalnız öneri metni ürettiğini doğruluyorum.
  it("creates a contract-safe key suggestion", () => {
    expect(suggestBannerKey("  Yaz İndirimi 2026! ")).toBe("Yaz-Indirimi-2026");
  });

  // Burada main seçiminin öğeyi aktif ve ilk sırada tutup diğer main işaretlerini kaldırdığını doğruluyorum.
  it("selects an active main item and moves it first", () => {
    const first = { ...createBannerItemDraft("a", true), name: "A" };
    const second = { ...createBannerItemDraft("b", false), name: "B", isActive: false, displayOrder: 1 };
    const result = selectMainBanner([first, second], "b");

    expect(result.map((item) => [item.clientId, item.isMain, item.isActive, item.displayOrder])).toEqual([
      ["b", true, true, 0],
      ["a", false, true, 1],
    ]);
  });

  // Burada main kayıt kaldırılırsa kalan ilk öğenin geçerli main olarak seçildiğini doğruluyorum.
  it("selects a replacement after removing the main item", () => {
    const first = createBannerItemDraft("a", true);
    const second = { ...createBannerItemDraft("b", false), isActive: false };
    const result = removeBannerItem([first, second], "a", true);

    expect(result).toHaveLength(1);
    expect(result[0]).toMatchObject({ clientId: "b", isMain: true, isActive: true, displayOrder: 0 });
  });

  // Burada sıra kontrollerinin main öğeyi yerinden oynatmadığını, alt öğeleri kararlı taşıdığını doğruluyorum.
  it("keeps main first while moving other items", () => {
    const items = [
      createBannerItemDraft("main", true),
      { ...createBannerItemDraft("a", false), displayOrder: 1 },
      { ...createBannerItemDraft("b", false), displayOrder: 2 },
    ];

    expect(moveBannerDraftItem(items, 0, 1, true).map((item) => item.clientId)).toEqual(["main", "a", "b"]);
    expect(moveBannerDraftItem(items, 2, -1, true).map((item) => item.clientId)).toEqual(["main", "b", "a"]);
  });

  // Burada yalnız bölüm öğelerinin gönderildiğini ve alt bölümlerde isMain değerinin false kaldığını doğruluyorum.
  it("maps one section to an isolated commit payload", () => {
    const item = { ...createBannerItemDraft("alt", true), name: "Alt", key: "alt", mediaUrl: "https://cdn.test/alt.jpg" };
    const result = toBannerCommitItems([item], false, new Map());

    expect(result).toEqual([expect.objectContaining({ key: "alt", isMain: false, displayOrder: 0 })]);
  });
});
