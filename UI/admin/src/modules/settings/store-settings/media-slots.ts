export const STORE_SETTINGS_MEDIA_SLOTS = {
  logo: { folder: "store-settings/logo" },
  darkLogo: { folder: "store-settings/dark-logo" },
  favicon: { folder: "store-settings/favicon" },
  defaultShareImage: { folder: "store-settings/share" },
  defaultOpenGraphImage: { folder: "store-settings/open-graph" },
} as const;

export type StoreSettingsMediaSlot = keyof typeof STORE_SETTINGS_MEDIA_SLOTS;

// Burada tarayıcıdan gelen medya anahtarını yalnız StoreSettings'e ait sabit klasörlerle sınırlandırıyorum.
export function isStoreSettingsMediaSlot(value: unknown): value is StoreSettingsMediaSlot {
  return typeof value === "string" && Object.hasOwn(STORE_SETTINGS_MEDIA_SLOTS, value);
}
