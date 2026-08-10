import "server-only";

import { apiRequest } from "@/lib/api/client";
import { ApiError } from "@/lib/api/problem";
import type { AdminSession } from "@/lib/auth/contracts";
import { BANNER_SECTION_KEYS, getBannerSectionConfig } from "@/modules/banners/section-config";
import type { BannerSection, BannerSectionKey, BannerSectionLoadResult, BannerSectionRequest } from "@/modules/banners/types";

// Burada tek banner bölümünün aktif ve pasif kayıtlarını yetkili no-store endpoint'inden okuyorum.
export function getAdminBannerSection(key: BannerSectionKey, session: AdminSession): Promise<BannerSection> {
  return apiRequest(getBannerSectionConfig(key).adminPath, { accessToken: session.accessToken });
}

// Burada altı bağımsız bölümün hatalarını birbirinden ayırarak yönetim ekranına birlikte taşıyorum.
export async function getAdminBannerSections(session: AdminSession): Promise<BannerSectionLoadResult[]> {
  const settled = await Promise.allSettled(
    BANNER_SECTION_KEYS.map((key) => getAdminBannerSection(key, session)),
  );

  return settled.map((result, index) => {
    const key = BANNER_SECTION_KEYS[index];
    if (result.status === "fulfilled") return { key, status: "success", section: result.value };
    return bannerSectionLoadError(key, result.reason);
  });
}

// Burada yalnız seçilen banner bölümünü generated request gövdesiyle atomik PUT endpoint'ine gönderiyorum.
export function updateBannerSection(
  key: BannerSectionKey,
  request: BannerSectionRequest,
  session: AdminSession,
): Promise<BannerSection> {
  return apiRequest(getBannerSectionConfig(key).publicPath, {
    method: "PUT",
    body: request,
    accessToken: session.accessToken,
  });
}

// Burada bölüm yükleme hatasını token veya iç adres taşımayan serileştirilebilir sonuca dönüştürüyorum.
function bannerSectionLoadError(key: BannerSectionKey, error: unknown): BannerSectionLoadResult {
  if (error instanceof ApiError) {
    return {
      key,
      status: "error",
      message: error.problem.detail || error.problem.title,
      traceId: error.problem.traceId,
    };
  }
  return { key, status: "error", message: "Banner bölümü yüklenemedi." };
}
