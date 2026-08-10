import type { CloudinaryAsset } from "../../../lib/cloudinary/browser-upload";
import type {
  BannerMediaType,
  BannerSectionCommitItem,
  BannerSectionItem,
} from "../types";

// Burada API kaydıyla yeni tarayıcı taslağını aynı kararlı kimlik altında taşıyorum.
export type BannerItemDraft = {
  clientId: string;
  id?: string;
  name: string;
  key: string;
  mediaUrl: string;
  mediaType: BannerMediaType;
  targetUrl: string;
  altText: string;
  displayOrder: number;
  isActive: boolean;
  isMain: boolean;
  file?: File;
  previewUrl?: string;
};

// Burada kayıtlı banner öğelerini API sırasını ve kullanıcı anahtarını değiştirmeden taslağa alıyorum.
export function toBannerItemDrafts(
  items: readonly BannerSectionItem[],
  isMainSection = false,
): BannerItemDraft[] {
  return [...items]
    .sort((left, right) => (
      (isMainSection ? Number(right.isMain) - Number(left.isMain) : 0)
      || left.displayOrder - right.displayOrder
    ))
    .map((item) => ({
      clientId: `existing-${item.id}`,
      id: item.id,
      name: item.name,
      key: item.key,
      mediaUrl: item.mediaUrl,
      mediaType: item.mediaType,
      targetUrl: item.targetUrl || "",
      altText: item.altText || "",
      displayOrder: item.displayOrder,
      isActive: item.isActive,
      isMain: item.isMain,
    }));
}

// Burada yeni banner taslağını yalnız boş bölümde main seçili olacak güvenli varsayımlarla oluşturuyorum.
export function createBannerItemDraft(clientId: string, selectAsMain: boolean): BannerItemDraft {
  return {
    clientId,
    name: "",
    key: "",
    mediaUrl: "",
    mediaType: 1,
    targetUrl: "",
    altText: "",
    displayOrder: 0,
    isActive: true,
    isMain: selectAsMain,
  };
}

// Burada ad alanından yalnız kullanıcı açıkça istediğinde uygulanacak, sözleşmeye uygun anahtar önerisi üretiyorum.
export function suggestBannerKey(name: string): string {
  return name
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/ı/g, "i")
    .replace(/İ/g, "I")
    .replace(/[^A-Za-z0-9_-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 100);
}

// Burada seçilen main öğeyi aktifleştirip ilk sıraya alırken diğer main işaretlerini kaldırıyorum.
export function selectMainBanner(items: readonly BannerItemDraft[], clientId: string): BannerItemDraft[] {
  const selected = items.find((item) => item.clientId === clientId);
  if (!selected) return [...items];
  const remaining = items.filter((item) => item.clientId !== clientId);
  return normalizeDisplayOrders([
    { ...selected, isActive: true, isMain: true },
    ...remaining.map((item) => ({ ...item, isMain: false })),
  ]);
}

// Burada silinen main öğeden sonra kalan ilk öğeyi aktif main olarak seçip boş bölümü geçerli bırakıyorum.
export function removeBannerItem(
  items: readonly BannerItemDraft[],
  clientId: string,
  isMainSection: boolean,
): BannerItemDraft[] {
  const removed = items.find((item) => item.clientId === clientId);
  const remaining = items.filter((item) => item.clientId !== clientId);
  if (isMainSection && removed?.isMain && remaining.length > 0) {
    return selectMainBanner(remaining, remaining[0].clientId);
  }
  return normalizeDisplayOrders(remaining.map((item) => ({
    ...item,
    isMain: isMainSection ? item.isMain : false,
  })));
}

// Burada main öğenin ilk sıra kuralını koruyarak banner öğesini bir adım taşıyorum.
export function moveBannerDraftItem(
  items: readonly BannerItemDraft[],
  index: number,
  direction: -1 | 1,
  isMainSection: boolean,
): BannerItemDraft[] {
  const target = index + direction;
  if (index < 0 || index >= items.length || target < 0 || target >= items.length) return [...items];
  if (isMainSection && (items[index].isMain || items[target].isMain)) return [...items];
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return normalizeDisplayOrders(next);
}

// Burada yerel taslağı yalnız seçilen bölümün Server Action commit modeline dönüştürüyorum.
export function toBannerCommitItems(
  items: readonly BannerItemDraft[],
  isMainSection: boolean,
  uploadedAssets: ReadonlyMap<string, CloudinaryAsset>,
): BannerSectionCommitItem[] {
  return items.map((item, index) => {
    const asset = uploadedAssets.get(item.clientId);
    return {
      name: item.name,
      key: item.key,
      mediaUrl: asset?.secureUrl || item.mediaUrl,
      mediaType: asset ? (asset.resourceType === "video" ? 2 : 1) : item.mediaType,
      targetUrl: item.targetUrl || null,
      altText: item.altText || null,
      displayOrder: index,
      isActive: item.isActive,
      isMain: isMainSection ? item.isMain : false,
      asset,
    };
  });
}

// Burada her taslak değişikliğinden sonra bölüm sırasını benzersiz 0..n-1 değerlerine getiriyorum.
function normalizeDisplayOrders(items: readonly BannerItemDraft[]): BannerItemDraft[] {
  return items.map((item, index) => ({ ...item, displayOrder: index }));
}
