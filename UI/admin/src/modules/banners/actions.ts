"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  getAdminBannerSection,
  updateBannerSection,
} from "@/modules/banners/api";
import {
  toBannerSectionRequest,
  validateUploadedBannerAssets,
  validateBannerSectionItems,
} from "@/modules/banners/media";
import { isBannerSectionKey } from "@/modules/banners/section-config";
import type {
  BannerActionResult,
  BannerSectionCommitInput,
  BannerSectionKey,
} from "@/modules/banners/types";

// Burada yalnız seçilen banner bölümünü doğrulayıp kendi atomik PUT işlemiyle kaydediyorum.
export async function updateBannerSectionAction(
  sectionKey: BannerSectionKey,
  input: BannerSectionCommitInput,
): Promise<BannerActionResult> {
  if (!isBannerSectionKey(sectionKey)) {
    return { status: "error", message: "Banner bölümü tanınamadı. Sayfayı yenileyip tekrar deneyin." };
  }
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return bannerActionError(error, "Yönetici oturumu doğrulanamadı");
  }
  if (!isBannerCommitInput(input)) {
    return { status: "error", message: "Banner verisi doğrulanamadı. Alanları kontrol edip tekrar deneyin." };
  }

  const validation = validateBannerSectionItems(sectionKey, input.items);
  if (!validation.valid) {
    return {
      status: "error",
      message: validation.message,
      fieldErrors: validation.fieldErrors,
    };
  }

  const uploadError = validateUploadedBannerAssets(
    sectionKey,
    input.items,
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim() || "",
  );
  if (uploadError) return { status: "error", message: uploadError };

  try {
    const section = await updateBannerSection(
      sectionKey,
      toBannerSectionRequest(sectionKey, input.items),
      session,
    );
    revalidatePath("/banners");
    return { status: "success", message: `${section.name} kaydedildi.`, section };
  } catch (error) {
    return bannerActionError(error, "Banner bölümü kaydedilemedi");
  }
}

// Burada istemciden gelen action girdisinin generated request alanlarını güvenli çalışma zamanında taşıdığını doğruluyorum.
function isBannerCommitInput(value: unknown): value is BannerSectionCommitInput {
  if (!value || typeof value !== "object" || !Array.isArray((value as { items?: unknown }).items)) return false;
  return (value as { items: unknown[] }).items.every((candidate) => {
    if (!candidate || typeof candidate !== "object") return false;
    const item = candidate as Record<string, unknown>;
    const asset = item.asset;
    const hasValidAsset = asset === undefined || (
      Boolean(asset)
      && typeof asset === "object"
      && typeof (asset as Record<string, unknown>).secureUrl === "string"
      && typeof (asset as Record<string, unknown>).publicId === "string"
      && (
        (asset as Record<string, unknown>).resourceType === "image"
        || (asset as Record<string, unknown>).resourceType === "video"
      )
    );
    return typeof item.name === "string"
      && typeof item.key === "string"
      && typeof item.mediaUrl === "string"
      && (item.mediaType === 1 || item.mediaType === 2)
      && (item.targetUrl === null || item.targetUrl === undefined || typeof item.targetUrl === "string")
      && (item.altText === null || item.altText === undefined || typeof item.altText === "string")
      && typeof item.displayOrder === "number"
      && typeof item.isActive === "boolean"
      && typeof item.isMain === "boolean"
      && hasValidAsset;
  });
}

// Burada hata alan bölümü diğer beş bölümün durumunu değiştirmeden yeniden okuyorum.
export async function reloadBannerSectionAction(
  sectionKey: BannerSectionKey,
): Promise<BannerActionResult> {
  if (!isBannerSectionKey(sectionKey)) {
    return { status: "error", message: "Banner bölümü tanınamadı. Sayfayı yenileyip tekrar deneyin." };
  }

  try {
    const section = await getAdminBannerSection(
      sectionKey,
      await requireAdminActionSession(),
    );
    return { status: "success", message: `${section.name} yeniden yüklendi.`, section };
  } catch (error) {
    return bannerActionError(error, "Banner bölümü yeniden yüklenemedi");
  }
}

// Burada yetki, doğrulama ve servis hatalarını taslağı koruyan güvenli action sonucuna dönüştürüyorum.
function bannerActionError(error: unknown, prefix: string): BannerActionResult {
  if (!(error instanceof ApiError)) {
    return { status: "error", message: `${prefix}. Lütfen tekrar deneyin.` };
  }

  const message = error.problem.status === 401
    ? "Oturumunuz sona erdi. Değişiklikleriniz korunuyor; yeniden giriş yaptıktan sonra tekrar deneyin."
    : error.problem.status === 403
      ? "Bu banner bölümünü değiştirmek için yönetici yetkiniz bulunmuyor."
      : error.problem.status === 409
        ? "Banner bölümü başka bir değişiklikle çakıştı. Güncel veriyi yeniden yükleyip tekrar deneyin."
        : `${prefix}: ${error.problem.detail || error.problem.title}`;

  return {
    status: "error",
    message,
    traceId: error.problem.traceId,
    fieldErrors: error.problem.errors,
  };
}
