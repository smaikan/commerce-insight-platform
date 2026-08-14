import type { components } from "@/generated/api";

// Burada public mağaza ayarlarını doğrudan üretilmiş OpenAPI sözleşmesine bağlıyorum.
export type PublicStoreSettings = components["schemas"]["PublicStoreSettingsDto"];
