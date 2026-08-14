import type {
  AdminStoreSettings,
  UpdateStoreContactRequest,
  UpdateStoreIdentityRequest,
  UpdateStoreLegalRequest,
  UpdateStoreSeoRequest,
  UpdateStorefrontPreferencesRequest,
} from "@/modules/settings/types";

export type StoreSettingsSection = "identity" | "contact" | "legal" | "seo" | "storefront";

type WithoutConcurrency<T> = Omit<T, "expectedConcurrencyToken">;

export type StoreSettingsDrafts = {
  identity: WithoutConcurrency<UpdateStoreIdentityRequest>;
  contact: WithoutConcurrency<UpdateStoreContactRequest>;
  legal: WithoutConcurrency<UpdateStoreLegalRequest>;
  seo: WithoutConcurrency<UpdateStoreSeoRequest>;
  storefront: WithoutConcurrency<UpdateStorefrontPreferencesRequest>;
};

export type StoreSettingsCommitInput = {
  [Section in StoreSettingsSection]: {
    section: Section;
    expectedConcurrencyToken: string;
    values: StoreSettingsDrafts[Section];
  }
}[StoreSettingsSection];

export type StoreSettingsActionResult = {
  status: "success" | "error" | "conflict";
  message: string;
  settings?: AdminStoreSettings;
  currentSettings?: AdminStoreSettings;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
};

export const STORE_SETTINGS_SECTIONS: ReadonlyArray<{
  key: StoreSettingsSection;
  label: string;
  description: string;
}> = [
  { key: "identity", label: "Mağaza kimliği", description: "Ad, açıklama ve marka görselleri" },
  { key: "contact", label: "İletişim", description: "İletişim kanalları ve görünürlük" },
  { key: "legal", label: "Yasal bilgiler", description: "Şirket ve vergi kayıtları" },
  { key: "seo", label: "SEO ve sosyal", description: "Arama görünümü ve sosyal hesaplar" },
  { key: "storefront", label: "Storefront", description: "Çalışma durumu ve katalog tercihleri" },
];

export function draftsFromSettings(settings: AdminStoreSettings): StoreSettingsDrafts {
  return {
    identity: {
      displayName: settings.displayName,
      shortDescription: settings.shortDescription ?? null,
      logoUrl: settings.logoUrl ?? null,
      darkLogoUrl: settings.darkLogoUrl ?? null,
      faviconUrl: settings.faviconUrl ?? null,
      defaultShareImageUrl: settings.defaultShareImageUrl ?? null,
    },
    contact: {
      supportEmail: settings.supportEmail ?? null,
      supportPhone: settings.supportPhone ?? null,
      whatsappNumber: settings.whatsappNumber ?? null,
      contactAddress: settings.contactAddress ?? null,
      workingHours: settings.workingHours ?? null,
      mapUrl: settings.mapUrl ?? null,
      showSupportEmail: settings.showSupportEmail,
      showSupportPhone: settings.showSupportPhone,
      showWhatsapp: settings.showWhatsapp,
      showContactAddress: settings.showContactAddress,
      showWorkingHours: settings.showWorkingHours,
      showMap: settings.showMap,
    },
    legal: {
      legalCompanyName: settings.legalCompanyName ?? null,
      taxOffice: settings.taxOffice ?? null,
      taxNumber: settings.taxNumber ?? null,
      nationalIdentityNumber: settings.nationalIdentityNumber ?? null,
      mersisNumber: settings.mersisNumber ?? null,
      tradeRegistryNumber: settings.tradeRegistryNumber ?? null,
      country: settings.country ?? null,
      city: settings.city ?? null,
      district: settings.district ?? null,
      addressLine: settings.addressLine ?? null,
      postalCode: settings.postalCode ?? null,
    },
    seo: {
      defaultTitle: settings.defaultTitle ?? null,
      titleTemplate: settings.titleTemplate ?? null,
      defaultDescription: settings.defaultDescription ?? null,
      defaultOpenGraphImageUrl: settings.defaultOpenGraphImageUrl ?? null,
      allowIndexing: settings.allowIndexing,
      facebookUrl: settings.facebookUrl ?? null,
      instagramUrl: settings.instagramUrl ?? null,
      tiktokUrl: settings.tiktokUrl ?? null,
      youtubeUrl: settings.youtubeUrl ?? null,
      xUrl: settings.xUrl ?? null,
      pinterestUrl: settings.pinterestUrl ?? null,
    },
    storefront: {
      status: settings.status,
      statusMessage: settings.statusMessage ?? null,
      showOutOfStockProducts: settings.showOutOfStockProducts,
      showProductsWithoutPrice: settings.showProductsWithoutPrice,
      defaultProductSort: settings.defaultProductSort,
      defaultProductSortDescending: settings.defaultProductSortDescending,
      showCompareAtPrice: settings.showCompareAtPrice,
      showStockWarning: settings.showStockWarning,
      lowStockThreshold: settings.lowStockThreshold,
    },
  };
}
