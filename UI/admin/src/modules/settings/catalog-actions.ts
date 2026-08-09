"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createCatalogItem, setCatalogItemActivation, updateCatalogItem } from "@/modules/settings/catalog-api";
import { catalogResourceConfigs, type CatalogResource } from "@/modules/settings/catalog-resource";
import type { CatalogFormValue } from "@/modules/settings/catalog-types";
import type { SettingsActionState } from "@/modules/settings/types";

// Burada yeni katalog tanımını kaynak alanlarına göre doğrulayıp gerçek create endpoint'ine gönderiyorum.
export async function createCatalogItemAction(resource: CatalogResource, previousState: SettingsActionState, formData: FormData): Promise<SettingsActionState> {
  void previousState;
  const parsed = parseCatalogForm(resource, formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await createCatalogItem(resource, parsed.value, session.value);
  } catch (error) {
    return actionError(error, `${catalogResourceConfigs[resource].singularTitle} oluşturulamadı`);
  }
  revalidatePath(`/settings/catalog/${resource}`);
  redirect(`/settings/catalog/${resource}?created=1`);
}

// Burada katalog kaydının bilgi alanlarını aktiflikten bağımsız güncelliyorum.
export async function updateCatalogItemAction(resource: CatalogResource, id: string, previousState: SettingsActionState, formData: FormData): Promise<SettingsActionState> {
  void previousState;
  const parsed = parseCatalogForm(resource, formData);
  if (!parsed.ok) return parsed.state;
  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await updateCatalogItem(resource, id, parsed.value, session.value);
  } catch (error) {
    return actionError(error, `${catalogResourceConfigs[resource].singularTitle} güncellenemedi`);
  }
  revalidatePath(`/settings/catalog/${resource}`);
  redirect(`/settings/catalog/${resource}?updated=1`);
}

// Burada katalog kaydının aktiflik durumunu bağımsız ve belgeli endpoint üzerinden değiştiriyorum.
export async function setCatalogItemActivationAction(resource: CatalogResource, id: string, isActive: boolean, previousState: SettingsActionState): Promise<SettingsActionState> {
  void previousState;
  const session = await actionSession();
  if (!session.ok) return session.state;
  try {
    await setCatalogItemActivation(resource, id, isActive, session.value);
    revalidatePath(`/settings/catalog/${resource}`);
    return { status: "success", message: isActive ? "Kayıt etkinleştirildi." : "Kayıt pasifleştirildi." };
  } catch (error) {
    return actionError(error, "Kayıt durumu değiştirilemedi");
  }
}

// Burada kaynak bazlı alanları backend doğrulama sınırlarıyla eşleştiriyorum.
function parseCatalogForm(resource: CatalogResource, formData: FormData): { ok: true; value: CatalogFormValue } | { ok: false; state: SettingsActionState } {
  const config = catalogResourceConfigs[resource];
  const name = String(formData.get("name") ?? "").trim();
  const url = config.supportsUrl ? String(formData.get("url") ?? "").trim() : "";
  const description = config.supportsDescription ? String(formData.get("description") ?? "").trim() : "";
  const fieldErrors: Record<string, string[]> = {};
  if (!name || name.length > 150) fieldErrors.name = ["Ad 1–150 karakter olmalıdır."];
  if (url.length > 200) fieldErrors.url = ["URL değeri en fazla 200 karakter olabilir."];
  if (description.length > 1000) fieldErrors.description = ["Açıklama en fazla 1000 karakter olabilir."];
  if (Object.keys(fieldErrors).length) return { ok: false, state: { status: "error", message: "İşaretli alanları kontrol edin.", fieldErrors } };
  return { ok: true, value: { name, url: url || null, description: description || null, isActive: formData.get("isActive") === "on" } };
}

// Burada her katalog mutation'ı öncesinde aktif Admin oturumunu yeniden doğruluyorum.
async function actionSession() {
  try {
    return { ok: true as const, value: await requireAdminActionSession() };
  } catch (error) {
    return { ok: false as const, state: actionError(error, "Yönetici oturumu doğrulanamadı") };
  }
}

// Burada API hatasını taslak veriyi koruyan ve takip kimliği taşıyan form sonucuna dönüştürüyorum.
function actionError(error: unknown, prefix: string): SettingsActionState {
  if (error instanceof ApiError) {
    const message = error.problem.status === 403
      ? "Bu işlem yalnızca aktif yönetici hesaplarına açıktır."
      : error.problem.status === 401
        ? "Oturumunuz sona erdi. Form veriniz korunuyor; yeniden giriş yapın."
        : `${prefix}: ${error.problem.detail || error.problem.title}`;
    return { status: "error", message, traceId: error.problem.traceId, fieldErrors: error.problem.errors };
  }
  return { status: "error", message: `${prefix}. Lütfen tekrar deneyin.` };
}
