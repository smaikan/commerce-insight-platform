import { isTrustedCloudinaryAsset, type CloudinaryAsset } from "../../lib/cloudinary/browser-upload";
import { getBannerSectionConfig } from "./section-config";
import type {
  BannerMediaKind,
  BannerMediaType,
  BannerSectionCommitItem,
  BannerSectionItemRequest,
  BannerSectionKey,
  BannerSectionRequest,
  BannerValidationResult,
} from "./types";

export const MAX_BANNER_ITEMS = 5;
export const MAX_BANNER_NAME_LENGTH = 150;
export const MAX_BANNER_KEY_LENGTH = 100;
export const MAX_BANNER_TEXT_LENGTH = 500;
export const BANNER_KEY_PATTERN = /^[A-Za-z0-9][A-Za-z0-9_-]*$/;

// Burada API medya enumunu önizleme katmanının açık tür etiketlerine dönüştürüyorum.
export function bannerMediaKind(mediaType: BannerMediaType): BannerMediaKind {
  if (mediaType === 1) return "image";
  if (mediaType === 2) return "video";
  return "unknown";
}

// Burada altı bölümün ortak alan, URL, benzersizlik ve main kurallarını PUT öncesinde birlikte doğruluyorum.
export function validateBannerSectionItems(
  sectionKey: BannerSectionKey,
  items: readonly BannerSectionCommitItem[],
): BannerValidationResult {
  const fieldErrors: Record<string, string[]> = {};
  const normalizedKeys = new Map<string, number>();
  const displayOrders = new Map<number, number>();
  const config = getBannerSectionConfig(sectionKey);

  if (items.length > MAX_BANNER_ITEMS) {
    fieldErrors.items = [`Bir bölümde en fazla ${MAX_BANNER_ITEMS} banner bulunabilir.`];
  }

  items.forEach((item, index) => {
    const prefix = `items.${index}`;
    const name = item.name.trim();
    const key = item.key.trim();
    const targetUrl = item.targetUrl?.trim() || "";
    const altText = item.altText?.trim() || "";

    if (!name || name.length > MAX_BANNER_NAME_LENGTH) {
      addFieldError(fieldErrors, `${prefix}.name`, `Ad 1–${MAX_BANNER_NAME_LENGTH} karakter olmalıdır.`);
    }
    if (!key || key.length > MAX_BANNER_KEY_LENGTH || !BANNER_KEY_PATTERN.test(key)) {
      addFieldError(fieldErrors, `${prefix}.key`, "Anahtar harf veya rakamla başlamalı; yalnız harf, rakam, _ ve - içermelidir.");
    }
    const normalizedKey = key.toLocaleLowerCase("en-US");
    const previousKeyIndex = normalizedKeys.get(normalizedKey);
    if (previousKeyIndex !== undefined) {
      addFieldError(fieldErrors, `${prefix}.key`, `Anahtar ${previousKeyIndex + 1}. kayıtla aynı olamaz.`);
    } else if (key) {
      normalizedKeys.set(normalizedKey, index);
    }

    if (!isAbsoluteHttpUrl(item.mediaUrl) || item.mediaUrl.length > MAX_BANNER_TEXT_LENGTH) {
      addFieldError(fieldErrors, `${prefix}.mediaUrl`, "Medya adresi en fazla 500 karakterlik mutlak HTTP/HTTPS URL olmalıdır.");
    }
    if (targetUrl && (!isValidTargetUrl(targetUrl) || targetUrl.length > MAX_BANNER_TEXT_LENGTH)) {
      addFieldError(fieldErrors, `${prefix}.targetUrl`, "Hedef, / ile başlayan uygulama yolu veya mutlak HTTP/HTTPS URL olmalıdır.");
    }
    if (altText.length > MAX_BANNER_TEXT_LENGTH) {
      addFieldError(fieldErrors, `${prefix}.altText`, "Alternatif metin en fazla 500 karakter olabilir.");
    }
    if (item.mediaType !== 1 && item.mediaType !== 2) {
      addFieldError(fieldErrors, `${prefix}.mediaType`, "Medya türü görsel veya video olmalıdır.");
    }
    if (!Number.isInteger(item.displayOrder) || item.displayOrder < 0) {
      addFieldError(fieldErrors, `${prefix}.displayOrder`, "Görüntüleme sırası sıfır veya pozitif tam sayı olmalıdır.");
    } else {
      const previousOrderIndex = displayOrders.get(item.displayOrder);
      if (previousOrderIndex !== undefined) {
        addFieldError(fieldErrors, `${prefix}.displayOrder`, `Görüntüleme sırası ${previousOrderIndex + 1}. kayıtla aynı olamaz.`);
      } else {
        displayOrders.set(item.displayOrder, index);
      }
    }
  });

  if (config.isMain && items.length > 0) {
    const selected = items.filter((item) => item.isMain);
    if (selected.length !== 1 || !selected[0]?.isActive) {
      fieldErrors.itemsMain = ["Main Banner bölümünde tam olarak bir aktif kayıt ana banner seçilmelidir."];
    }
  }
  if (!config.isMain && items.some((item) => item.isMain)) {
    fieldErrors.itemsMain = ["Alt banner bölümlerinde ana banner seçimi kullanılamaz."];
  }

  return Object.keys(fieldErrors).length === 0
    ? { valid: true }
    : { valid: false, message: "Banner bölümündeki alanları kontrol edin.", fieldErrors };
}

// Burada main seçimini ilk sıraya, diğer kayıtları kararlı displayOrder sırasına taşıyorum.
export function sortBannerSectionItems<T extends Pick<BannerSectionItemRequest, "displayOrder" | "isMain">>(
  sectionKey: BannerSectionKey,
  items: readonly T[],
): T[] {
  const isMainSection = getBannerSectionConfig(sectionKey).isMain;
  return items
    .map((item, index) => ({ item, index }))
    .sort((left, right) => {
      if (isMainSection && left.item.isMain !== right.item.isMain) return left.item.isMain ? -1 : 1;
      return left.item.displayOrder - right.item.displayOrder || left.index - right.index;
    })
    .map(({ item }) => item);
}

// Burada doğrulanmış commit modelini asset kanıtını dışarıda bırakan generated BannerSectionRequest gövdesine dönüştürüyorum.
export function toBannerSectionRequest(
  sectionKey: BannerSectionKey,
  items: readonly BannerSectionCommitItem[],
): BannerSectionRequest {
  const isMainSection = getBannerSectionConfig(sectionKey).isMain;
  return {
    items: sortBannerSectionItems(sectionKey, items).map((item, index): BannerSectionItemRequest => ({
      name: item.name.trim(),
      key: item.key.trim(),
      mediaUrl: item.mediaUrl.trim(),
      mediaType: item.mediaType,
      targetUrl: nullableText(item.targetUrl),
      altText: nullableText(item.altText),
      displayOrder: index,
      isActive: item.isActive,
      isMain: isMainSection ? item.isMain : false,
    })),
  };
}

// Burada yalnız yeni yükleme kanıtı taşıyan kayıtların URL, tür, hesap ve bölüm klasörü eşleşmesini doğruluyorum.
export function validateUploadedBannerAssets(
  sectionKey: BannerSectionKey,
  items: readonly BannerSectionCommitItem[],
  cloudName: string,
): string | null {
  const folder = getBannerSectionConfig(sectionKey).folder;
  for (const item of items) {
    if (!item.asset) continue;
    const expectedResourceType = item.mediaType === 2 ? "video" : item.mediaType === 1 ? "image" : null;
    if (
      !expectedResourceType
      || item.mediaUrl !== item.asset.secureUrl
      || item.asset.resourceType !== expectedResourceType
      || !isTrustedCloudinaryAsset(item.asset, {
        folder,
        cloudName,
        allowedResourceTypes: ["image", "video"],
      })
    ) {
      return `${item.name || "Banner"} yükleme kaynağı doğrulanamadı.`;
    }
  }
  return null;
}

// Burada başarılı yüklemeleri yeniden deneme planından çıkarıp yalnız eksik medya anahtarlarını döndürüyorum.
export function pendingBannerUploadKeys(keys: string[], uploadedKeys: Iterable<string>): string[] {
  const uploaded = new Set(uploadedKeys);
  return keys.filter((key) => !uploaded.has(key));
}

// Burada erişilebilir sıra kontrolleri için bir banner kaydını sınırlar içinde tek adım taşıyorum.
export function moveBannerItem<T>(items: T[], index: number, direction: -1 | 1): T[] {
  const target = index + direction;
  if (index < 0 || index >= items.length || target < 0 || target >= items.length) return items;
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

// Burada Cloudinary yükleme varlığını commit modelinin medya alanlarıyla birleştirmek için kanıt olarak koruyorum.
export function withUploadedBannerAsset(
  item: Omit<BannerSectionCommitItem, "mediaUrl" | "mediaType" | "asset">,
  asset: CloudinaryAsset,
): BannerSectionCommitItem {
  return { ...item, mediaUrl: asset.secureUrl, mediaType: asset.resourceType === "video" ? 2 : 1, asset };
}

// Burada aynı alana birden fazla sözleşme hatası eklenebilmesini koruyorum.
function addFieldError(errors: Record<string, string[]>, field: string, message: string): void {
  (errors[field] ||= []).push(message);
}

// Burada medya URL'sinin yalnız mutlak HTTP veya HTTPS adresi olduğunu doğruluyorum.
function isAbsoluteHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return (url.protocol === "http:" || url.protocol === "https:") && Boolean(url.hostname);
  } catch {
    return false;
  }
}

// Burada banner hedefinin güvenli uygulama yolu veya mutlak web adresi olmasını sağlıyorum.
function isValidTargetUrl(value: string): boolean {
  return (value.startsWith("/") && !value.startsWith("//")) || isAbsoluteHttpUrl(value);
}

// Burada opsiyonel metinlerin boş değerlerini wire sözleşmesindeki null biçimine getiriyorum.
function nullableText(value: string | null | undefined): string | null {
  const normalized = value?.trim();
  return normalized ? normalized : null;
}
