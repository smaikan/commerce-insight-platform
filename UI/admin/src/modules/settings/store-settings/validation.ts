import type { StoreSettingsCommitInput, StoreSettingsDrafts, StoreSettingsSection } from "./types";

type ParsedCommit =
  | { ok: true; value: StoreSettingsCommitInput }
  | { ok: false; fieldErrors: Record<string, string[]> };

export function parseStoreSettingsCommit(input: unknown): ParsedCommit {
  const root = record(input);
  const section = root?.section;
  const values = record(root?.values);
  const token = typeof root?.expectedConcurrencyToken === "string" ? root.expectedConcurrencyToken.trim() : "";
  const errors: Record<string, string[]> = {};

  if (!isSection(section)) errors.section = ["Geçerli bir ayar bölümü seçin."];
  if (!UUID_PATTERN.test(token)) errors.expectedConcurrencyToken = ["Ayar sürümü doğrulanamadı. Sayfayı yenileyin."];
  if (!values) errors.values = ["Ayar bilgileri okunamadı."];
  if (!isSection(section) || !values || Object.keys(errors).length) return { ok: false, fieldErrors: errors };

  const parsed = parseSection(section, values);
  if (!parsed.ok) return parsed;
  return {
    ok: true,
    value: { section, expectedConcurrencyToken: token, values: parsed.value } as StoreSettingsCommitInput,
  };
}

function parseSection<Section extends StoreSettingsSection>(section: Section, values: Record<string, unknown>): { ok: true; value: StoreSettingsDrafts[Section] } | { ok: false; fieldErrors: Record<string, string[]> } {
  const errors: Record<string, string[]> = {};

  if (section === "identity") {
    const displayName = text(values.displayName);
    maxRequired(displayName, "displayName", 150, "Mağaza adı", errors);
    const value = {
      displayName,
      shortDescription: optionalText(values.shortDescription, "shortDescription", 500, "Kısa açıklama", errors),
      logoUrl: optionalUrl(values.logoUrl, "logoUrl", errors),
      darkLogoUrl: optionalUrl(values.darkLogoUrl, "darkLogoUrl", errors),
      faviconUrl: optionalUrl(values.faviconUrl, "faviconUrl", errors),
      defaultShareImageUrl: optionalUrl(values.defaultShareImageUrl, "defaultShareImageUrl", errors),
    };
    return result(value as StoreSettingsDrafts[Section], errors);
  }

  if (section === "contact") {
    const supportEmail = optionalText(values.supportEmail, "supportEmail", 320, "Destek e-postası", errors);
    if (supportEmail && !EMAIL_PATTERN.test(supportEmail)) errors.supportEmail = ["Geçerli bir e-posta adresi girin."];
    const value = {
      supportEmail,
      supportPhone: optionalText(values.supportPhone, "supportPhone", 30, "Destek telefonu", errors),
      whatsappNumber: optionalText(values.whatsappNumber, "whatsappNumber", 30, "WhatsApp numarası", errors),
      contactAddress: optionalText(values.contactAddress, "contactAddress", 1000, "İletişim adresi", errors),
      workingHours: optionalText(values.workingHours, "workingHours", 500, "Çalışma saatleri", errors),
      mapUrl: optionalUrl(values.mapUrl, "mapUrl", errors),
      showSupportEmail: bool(values.showSupportEmail, "showSupportEmail", errors),
      showSupportPhone: bool(values.showSupportPhone, "showSupportPhone", errors),
      showWhatsapp: bool(values.showWhatsapp, "showWhatsapp", errors),
      showContactAddress: bool(values.showContactAddress, "showContactAddress", errors),
      showWorkingHours: bool(values.showWorkingHours, "showWorkingHours", errors),
      showMap: bool(values.showMap, "showMap", errors),
    };
    return result(value as StoreSettingsDrafts[Section], errors);
  }

  if (section === "legal") {
    const value = {
      legalCompanyName: optionalText(values.legalCompanyName, "legalCompanyName", 200, "Yasal şirket adı", errors),
      taxOffice: optionalText(values.taxOffice, "taxOffice", 150, "Vergi dairesi", errors),
      taxNumber: optionalText(values.taxNumber, "taxNumber", 50, "Vergi numarası", errors),
      nationalIdentityNumber: optionalText(values.nationalIdentityNumber, "nationalIdentityNumber", 50, "T.C. kimlik numarası", errors),
      mersisNumber: optionalText(values.mersisNumber, "mersisNumber", 50, "MERSİS numarası", errors),
      tradeRegistryNumber: optionalText(values.tradeRegistryNumber, "tradeRegistryNumber", 50, "Ticaret sicil numarası", errors),
      country: optionalText(values.country, "country", 150, "Ülke", errors),
      city: optionalText(values.city, "city", 150, "Şehir", errors),
      district: optionalText(values.district, "district", 150, "İlçe", errors),
      addressLine: optionalText(values.addressLine, "addressLine", 1000, "Şirket adresi", errors),
      postalCode: optionalText(values.postalCode, "postalCode", 20, "Posta kodu", errors),
    };
    return result(value as StoreSettingsDrafts[Section], errors);
  }

  if (section === "seo") {
    const titleTemplate = optionalText(values.titleTemplate, "titleTemplate", 250, "Başlık şablonu", errors);
    if (titleTemplate && (titleTemplate.match(/%s/g)?.length ?? 0) !== 1) {
      errors.titleTemplate = ["Başlık şablonu tam olarak bir %s yer tutucusu içermelidir."];
    }
    const value = {
      defaultTitle: optionalText(values.defaultTitle, "defaultTitle", 200, "Varsayılan başlık", errors),
      titleTemplate,
      defaultDescription: optionalText(values.defaultDescription, "defaultDescription", 500, "Varsayılan açıklama", errors),
      defaultOpenGraphImageUrl: optionalUrl(values.defaultOpenGraphImageUrl, "defaultOpenGraphImageUrl", errors),
      allowIndexing: bool(values.allowIndexing, "allowIndexing", errors),
      facebookUrl: optionalUrl(values.facebookUrl, "facebookUrl", errors),
      instagramUrl: optionalUrl(values.instagramUrl, "instagramUrl", errors),
      tiktokUrl: optionalUrl(values.tiktokUrl, "tiktokUrl", errors),
      youtubeUrl: optionalUrl(values.youtubeUrl, "youtubeUrl", errors),
      xUrl: optionalUrl(values.xUrl, "xUrl", errors),
      pinterestUrl: optionalUrl(values.pinterestUrl, "pinterestUrl", errors),
    };
    return result(value as StoreSettingsDrafts[Section], errors);
  }

  const status = integer(values.status);
  const defaultProductSort = integer(values.defaultProductSort);
  const lowStockThreshold = integer(values.lowStockThreshold);
  if (status === null || ![0, 1, 2].includes(status)) errors.status = ["Geçerli bir mağaza durumu seçin."];
  if (defaultProductSort === null || ![0, 1, 2, 3].includes(defaultProductSort)) errors.defaultProductSort = ["Geçerli bir ürün sıralaması seçin."];
  if (lowStockThreshold === null || lowStockThreshold < 1 || lowStockThreshold > 1_000_000) errors.lowStockThreshold = ["Düşük stok eşiği 1–1.000.000 arasında olmalıdır."];
  const value = {
    status: status as 0 | 1 | 2,
    statusMessage: optionalText(values.statusMessage, "statusMessage", 500, "Durum mesajı", errors),
    showOutOfStockProducts: bool(values.showOutOfStockProducts, "showOutOfStockProducts", errors),
    showProductsWithoutPrice: bool(values.showProductsWithoutPrice, "showProductsWithoutPrice", errors),
    defaultProductSort: defaultProductSort as 0 | 1 | 2 | 3,
    defaultProductSortDescending: bool(values.defaultProductSortDescending, "defaultProductSortDescending", errors),
    showCompareAtPrice: bool(values.showCompareAtPrice, "showCompareAtPrice", errors),
    showStockWarning: bool(values.showStockWarning, "showStockWarning", errors),
    lowStockThreshold: lowStockThreshold ?? 0,
  };
  return result(value as StoreSettingsDrafts[Section], errors);
}

function optionalText(value: unknown, field: string, max: number, label: string, errors: Record<string, string[]>): string | null {
  const normalized = text(value);
  if (!normalized) return null;
  if (normalized.length > max) errors[field] = [`${label} en fazla ${max.toLocaleString("tr-TR")} karakter olabilir.`];
  return normalized;
}

function optionalUrl(value: unknown, field: string, errors: Record<string, string[]>): string | null {
  const normalized = optionalText(value, field, 500, "URL", errors);
  if (!normalized) return null;
  try {
    const url = new URL(normalized);
    if (url.protocol !== "http:" && url.protocol !== "https:") throw new Error();
  } catch {
    errors[field] = ["Mutlak bir HTTP veya HTTPS adresi girin."];
  }
  return normalized;
}

function maxRequired(value: string, field: string, max: number, label: string, errors: Record<string, string[]>) {
  if (!value) errors[field] = [`${label} zorunludur.`];
  else if (value.length > max) errors[field] = [`${label} en fazla ${max} karakter olabilir.`];
}

function bool(value: unknown, field: string, errors: Record<string, string[]>): boolean {
  if (typeof value !== "boolean") errors[field] = ["Bu tercih için geçerli bir değer seçin."];
  return value === true;
}

function integer(value: unknown): number | null {
  return typeof value === "number" && Number.isInteger(value) ? value : null;
}

function text(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

function record(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : null;
}

function result<T>(value: T, errors: Record<string, string[]>): { ok: true; value: T } | { ok: false; fieldErrors: Record<string, string[]> } {
  return Object.keys(errors).length ? { ok: false, fieldErrors: errors } : { ok: true, value };
}

function isSection(value: unknown): value is StoreSettingsSection {
  return value === "identity" || value === "contact" || value === "legal" || value === "seo" || value === "storefront";
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
// Burada .NET Guid sözleşmesini sürüm/varyant dayatmadan yalnız kanonik 8-4-4-4-12 biçiminde doğruluyorum.
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
