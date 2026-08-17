"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  getAdminStoreSettings,
  updateStoreContact,
  updateStoreIdentity,
  updateStoreLegal,
  updateStoreSeo,
  updateStorefrontPreferences,
} from "@/modules/settings/api";
import type { StoreSettingsActionResult, StoreSettingsCommitInput } from "@/modules/settings/store-settings/types";
import { parseStoreSettingsCommit } from "@/modules/settings/store-settings/validation";

export async function saveStoreSettingsSectionAction(input: StoreSettingsCommitInput): Promise<StoreSettingsActionResult> {
  const parsed = parseStoreSettingsCommit(input);
  if (!parsed.ok) {
    const firstError = Object.values(parsed.fieldErrors).flat()[0];
    return { status: "error", message: firstError || "Lütfen işaretli alanları kontrol edin.", fieldErrors: parsed.fieldErrors };
  }

  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return actionError(error, "Yönetici oturumu doğrulanamadı.");
  }

  try {
    const { section, expectedConcurrencyToken, values } = parsed.value;
    const settings = section === "identity"
      ? await updateStoreIdentity({ ...values, expectedConcurrencyToken }, session)
      : section === "contact"
        ? await updateStoreContact({ ...values, expectedConcurrencyToken }, session)
        : section === "legal"
          ? await updateStoreLegal({ ...values, expectedConcurrencyToken }, session)
          : section === "seo"
            ? await updateStoreSeo({ ...values, expectedConcurrencyToken }, session)
            : await updateStorefrontPreferences({ ...values, expectedConcurrencyToken }, session);

    revalidatePath("/settings/store");
    return { status: "success", message: "Değişiklikler kaydedildi.", settings };
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 409 && error.problem.code === "concurrency_conflict") {
      const currentSettings = await getAdminStoreSettings(session).catch(() => undefined);
      return {
        status: "conflict",
        message: "Ayarlar siz düzenlerken başka bir oturumda değiştirildi. Güncel veriyi yükleyip değişikliklerinizi yeniden uygulayın.",
        currentSettings,
        traceId: error.problem.traceId,
      };
    }
    return actionError(error, "Mağaza ayarları kaydedilemedi.");
  }
}

export async function reloadStoreSettingsAction(): Promise<StoreSettingsActionResult> {
  try {
    const session = await requireAdminActionSession();
    const settings = await getAdminStoreSettings(session);
    return { status: "success", message: "Güncel ayarlar yüklendi.", settings };
  } catch (error) {
    return actionError(error, "Güncel mağaza ayarları alınamadı.");
  }
}

function actionError(error: unknown, fallback: string): StoreSettingsActionResult {
  if (error instanceof ApiError) {
    return {
      status: "error",
      message: error.problem.detail || fallback,
      traceId: error.problem.traceId,
      fieldErrors: error.problem.errors,
    };
  }
  return { status: "error", message: fallback };
}
