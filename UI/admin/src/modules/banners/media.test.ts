import { describe, expect, it } from "vitest";
import type { CloudinaryAsset } from "../../lib/cloudinary/browser-upload";
import {
  bannerMediaKind,
  moveBannerItem,
  pendingBannerUploadKeys,
  toBannerSectionRequest,
  validateBannerSectionItems,
  validateUploadedBannerAssets,
  withUploadedBannerAsset,
} from "./media";
import type { BannerSectionCommitItem } from "./types";

// Burada sözleşme testlerinin geçerli temel banner kaydını tek noktadan üretiyorum.
function item(overrides: Partial<BannerSectionCommitItem> = {}): BannerSectionCommitItem {
  return {
    name: "Yaz kampanyası",
    key: "summer-campaign",
    mediaUrl: "https://cdn.example.com/banner.webp",
    mediaType: 1,
    targetUrl: "/collections/yaz",
    altText: "Yaz koleksiyonu",
    displayOrder: 0,
    isActive: true,
    isMain: false,
    ...overrides,
  };
}

describe("banner section contract", () => {
  // Burada wire medya enumunun görünüm türlerine birebir dönüştüğünü doğruluyorum.
  it("maps media enum values", () => {
    expect(bannerMediaKind(1)).toBe("image");
    expect(bannerMediaKind(2)).toBe("video");
  });

  // Burada bölüm başına beş kayıt, benzersiz sıra ve büyük-küçük harf duyarsız anahtar kurallarını doğruluyorum.
  it("validates count and section-level uniqueness", () => {
    const duplicate = [
      item({ key: "Campaign", displayOrder: 0 }),
      item({ key: "campaign", displayOrder: 0 }),
    ];
    const duplicateResult = validateBannerSectionItems("alt-banner-1", duplicate);
    expect(duplicateResult.valid).toBe(false);
    if (!duplicateResult.valid) {
      expect(duplicateResult.fieldErrors["items.1.key"]).toBeDefined();
      expect(duplicateResult.fieldErrors["items.1.displayOrder"]).toBeDefined();
    }
    expect(validateBannerSectionItems("alt-banner-1", Array.from({ length: 6 }, (_, index) => item({ key: `key-${index}`, displayOrder: index }))).valid).toBe(false);
  });

  // Burada anahtar biçimi, mutlak medya URL'si ve güvenli hedef URL sınırlarını doğruluyorum.
  it("validates key and URL formats", () => {
    const result = validateBannerSectionItems("alt-banner-2", [item({
      key: "-invalid",
      mediaUrl: "/relative-media",
      targetUrl: "javascript:alert(1)",
    })]);
    expect(result.valid).toBe(false);
    if (!result.valid) {
      expect(result.fieldErrors["items.0.key"]).toBeDefined();
      expect(result.fieldErrors["items.0.mediaUrl"]).toBeDefined();
      expect(result.fieldErrors["items.0.targetUrl"]).toBeDefined();
    }
    expect(validateBannerSectionItems("alt-banner-2", [item({ targetUrl: "https://example.com/sale" })])).toEqual({ valid: true });
  });

  // Burada main bölümünün tek aktif ana seçim, alt bölümlerin ise isMain=false kuralını koruduğunu doğruluyorum.
  it("enforces main selection rules per section", () => {
    expect(validateBannerSectionItems("main-banner", [item({ isMain: false })]).valid).toBe(false);
    expect(validateBannerSectionItems("main-banner", [item({ isMain: true, isActive: false })]).valid).toBe(false);
    expect(validateBannerSectionItems("main-banner", [item({ isMain: true })])).toEqual({ valid: true });
    expect(validateBannerSectionItems("alt-banner-3", [item({ isMain: true })]).valid).toBe(false);
  });

  // Burada main seçimini ilk sıraya alıp sıraları normalize ederken Cloudinary asset kanıtını wire gövdesinden çıkardığımı doğruluyorum.
  it("builds generated request payload without upload evidence", () => {
    const asset: CloudinaryAsset = {
      secureUrl: "https://res.cloudinary.com/demo/video/upload/v1/banners/main-banner/hero.mp4",
      publicId: "banners/main-banner/hero",
      resourceType: "video",
    };
    const uploaded = withUploadedBannerAsset({
      name: " Hero ",
      key: " hero ",
      targetUrl: " ",
      altText: " Ana video ",
      displayOrder: 5,
      isActive: true,
      isMain: true,
    }, asset);
    const request = toBannerSectionRequest("main-banner", [
      item({ name: "İkinci", key: "second", displayOrder: 1 }),
      uploaded,
    ]);

    expect(request).toEqual({
      items: [
        { name: "Hero", key: "hero", mediaUrl: asset.secureUrl, mediaType: 2, targetUrl: null, altText: "Ana video", displayOrder: 0, isActive: true, isMain: true },
        { name: "İkinci", key: "second", mediaUrl: "https://cdn.example.com/banner.webp", mediaType: 1, targetUrl: "/collections/yaz", altText: "Yaz koleksiyonu", displayOrder: 1, isActive: true, isMain: false },
      ],
    });
    expect("asset" in (request.items?.[0] || {})).toBe(false);
  });

  // Burada yeni yükleme kanıtının bölüm klasörü, Cloudinary hesabı, URL ve wire medya türüyle eşleşmesini doğruluyorum.
  it("validates uploaded asset evidence while allowing existing manual URLs", () => {
    const trustedAsset: CloudinaryAsset = {
      secureUrl: "https://res.cloudinary.com/demo/image/upload/v1/banners/alt-banner-1/tile.webp",
      publicId: "banners/alt-banner-1/tile",
      resourceType: "image",
    };
    expect(validateUploadedBannerAssets("alt-banner-1", [item({ mediaUrl: "https://legacy.example.com/banner.jpg" })], "demo")).toBeNull();
    expect(validateUploadedBannerAssets("alt-banner-1", [item({ mediaUrl: trustedAsset.secureUrl, asset: trustedAsset })], "demo")).toBeNull();
    expect(validateUploadedBannerAssets("alt-banner-2", [item({ mediaUrl: trustedAsset.secureUrl, asset: trustedAsset })], "demo")).toMatch(/doğrulanamadı/);
    expect(validateUploadedBannerAssets("alt-banner-1", [item({ mediaUrl: trustedAsset.secureUrl, mediaType: 2, asset: trustedAsset })], "demo")).toMatch(/doğrulanamadı/);
  });

  // Burada yeniden deneme ve tek adımlı sıra yardımcılarının sınırlar içinde kararlı kaldığını doğruluyorum.
  it("keeps upload retries and item moves deterministic", () => {
    expect(pendingBannerUploadKeys(["a", "b", "c"], ["a", "c"])).toEqual(["b"]);
    expect(moveBannerItem(["a", "b", "c"], 1, -1)).toEqual(["b", "a", "c"]);
    expect(moveBannerItem(["a", "b", "c"], 2, 1)).toEqual(["a", "b", "c"]);
  });
});
