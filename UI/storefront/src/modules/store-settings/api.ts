import "server-only";

import { cache } from "react";

import { apiGet } from "@/lib/api/client";
import type { PublicStoreSettings } from "@/modules/store-settings/types";

// Burada ortak header/footer renderı boyunca public mağaza ayarlarını tek, kısa süreli ve etiketli okumayla paylaştırıyorum.
export const getPublicStoreSettings = cache(async (): Promise<PublicStoreSettings> => (
  apiGet<PublicStoreSettings>("/api/store-settings", {
    revalidate: 60,
    tags: ["store-settings"],
  })
));
