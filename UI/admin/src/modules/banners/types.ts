import type { components } from "@/generated/api";
import type { CloudinaryAsset } from "@/lib/cloudinary/browser-upload";
import type { BannerSectionKey } from "@/modules/banners/section-config";

// Burada banner response ve request modellerini doğrudan üretilen OpenAPI sözleşmesine bağlıyorum.
export type BannerSection = components["schemas"]["BannerSectionDto"];
export type BannerSectionItem = components["schemas"]["BannerItemDto"];
export type BannerSectionRequest = components["schemas"]["BannerSectionRequest"];
export type BannerSectionItemRequest = components["schemas"]["BannerItemRequest"];
export type BannerMediaType = components["schemas"]["BannerMediaType"];
export type { BannerSectionKey };

// Burada yeni yükleme kanıtını wire sözleşmesine taşımadan Server Action doğrulamasına açık tutuyorum.
export type BannerSectionCommitItem = BannerSectionItemRequest & {
  asset?: CloudinaryAsset;
};

// Burada tek bağımsız bölümün atomik kaydetme girdisini tanımlıyorum.
export type BannerSectionCommitInput = {
  items: BannerSectionCommitItem[];
};

// Burada yedi bağımsız bölümün paralel yükleme sonucunu serileştirilebilir bir union olarak tutuyorum.
export type BannerSectionLoadResult =
  | { key: BannerSectionKey; status: "success"; section: BannerSection }
  | { key: BannerSectionKey; status: "error"; message: string; traceId?: string };

// Burada banner mutation sonucunu istemciye yalnız güvenli alanlarla döndürüyorum.
export type BannerActionResult = {
  status: "success" | "error";
  message: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
  section?: BannerSection;
};

// Burada önizleme katmanının tanıdığı medya türlerini API enumundan bağımsız açık etiketlerle sınırlıyorum.
export type BannerMediaKind = "image" | "video" | "unknown";

// Burada pure sözleşme doğrulamasının alan bazlı hata taşıyan sonucunu tanımlıyorum.
export type BannerValidationResult =
  | { valid: true }
  | { valid: false; message: string; fieldErrors: Record<string, string[]> };
