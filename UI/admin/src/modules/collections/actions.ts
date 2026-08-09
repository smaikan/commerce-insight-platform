"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { components } from "@/generated/api";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  createCollection,
  setCollectionActivation,
  setCollectionFeatured,
  updateCollection,
} from "@/modules/collections/api";

type CollectionFormValue = components["schemas"]["CollectionRequest"];

// Burada yalnız manuel koleksiyonların belgelenmiş oluşturma gövdesini doğrulayıp kaydediyorum.
export async function createManualCollectionAction(formData: FormData): Promise<void> {
  const parsed = parseCollectionForm(formData);
  if (!parsed.ok) redirect("/collections/new?error=validation");

  try {
    await createCollection({
      ...parsed.value,
      isActive: formData.get("isActive") === "on",
      isFeatured: formData.get("isFeatured") === "on",
    }, await requireAdminActionSession());
  } catch (error) {
    redirect(`/collections/new?error=${collectionErrorCode(error)}`);
  }

  revalidateCollectionRoutes();
  redirect("/collections?created=1");
}

// Burada mevcut koleksiyonun ad, bağlantı, açıklama ve sıralama alanlarını güncelliyorum.
export async function updateManualCollectionAction(id: string, formData: FormData): Promise<void> {
  const parsed = parseCollectionForm(formData);
  if (!parsed.ok) redirect(`/collections/${encodeURIComponent(id)}?error=validation`);

  try {
    await updateCollection(id, parsed.value, await requireAdminActionSession());
  } catch (error) {
    redirect(`/collections/${encodeURIComponent(id)}?error=${collectionErrorCode(error)}`);
  }

  revalidateCollectionRoutes();
  redirect(`/collections/${encodeURIComponent(id)}?updated=1`);
}

// Burada koleksiyonun aktiflik durumunu içerik güncellemesinden bağımsız değiştiriyorum.
export async function setCollectionActivationAction(id: string, isActive: boolean): Promise<void> {
  try {
    await setCollectionActivation(id, isActive, await requireAdminActionSession());
  } catch (error) {
    redirect(`/collections/${encodeURIComponent(id)}?error=${collectionErrorCode(error)}`);
  }

  revalidateCollectionRoutes();
  redirect(`/collections/${encodeURIComponent(id)}?status=activation`);
}

// Burada koleksiyonun vitrin durumunu içerik güncellemesinden bağımsız değiştiriyorum.
export async function setCollectionFeaturedAction(id: string, isFeatured: boolean): Promise<void> {
  try {
    await setCollectionFeatured(id, isFeatured, await requireAdminActionSession());
  } catch (error) {
    redirect(`/collections/${encodeURIComponent(id)}?error=${collectionErrorCode(error)}`);
  }

  revalidateCollectionRoutes();
  redirect(`/collections/${encodeURIComponent(id)}?status=featured`);
}

// Burada koleksiyon formunu backend doğrulayıcılarıyla aynı uzunluk ve sıra sınırlarında okuyorum.
function parseCollectionForm(formData: FormData): { ok: true; value: CollectionFormValue } | { ok: false } {
  const name = text(formData, "name");
  const url = text(formData, "url");
  const description = text(formData, "description");
  const displayOrder = Number(text(formData, "displayOrder") || "0");
  const valid = Boolean(name)
    && name.length <= 150
    && url.length <= 200
    && description.length <= 1000
    && Number.isInteger(displayOrder)
    && displayOrder >= 0;
  if (!valid) return { ok: false };
  return { ok: true, value: { name, url: url || null, description: description || null, displayOrder } };
}

// Burada API hatasını form URL'sinde güvenle gösterilebilecek sınırlı bir hata koduna dönüştürüyorum.
function collectionErrorCode(error: unknown): string {
  if (!(error instanceof ApiError)) return "failed";
  if (error.problem.status === 401) return "session";
  if (error.problem.status === 403) return "forbidden";
  if (error.problem.status === 404) return "not-found";
  if (error.problem.status === 409) return "conflict";
  return "failed";
}

// Burada koleksiyon ve ürün ekranlarının değişiklikten sonra yetkili veriyi yeniden okumasını sağlıyorum.
function revalidateCollectionRoutes(): void {
  revalidatePath("/collections");
  revalidatePath("/products");
}

// Burada tekil form metnini boşlukları ayıklanmış biçimde alıyorum.
function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}
