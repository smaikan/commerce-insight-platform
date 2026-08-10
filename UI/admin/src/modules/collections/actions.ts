"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { CloudinaryAsset } from "@/lib/cloudinary/browser-upload";
import { adminMutationError } from "@/lib/admin/mutation-error";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  createCollection,
  deleteCollection,
  getCollection,
  setCollectionActivation,
  setCollectionFeatured,
  updateCollection,
} from "@/modules/collections/api";
import { isTrustedCollectionImageAsset, parseCollectionForm } from "@/modules/collections/form-data";
import type { CollectionActionState } from "@/modules/collections/types";

// Burada manuel koleksiyonu önce gerçek kimliğiyle oluşturup görsel yüklemesi için kimliği istemciye döndürüyorum.
export async function createManualCollectionAction(
  _previousState: CollectionActionState,
  formData: FormData,
): Promise<CollectionActionState> {
  const parsed = parseCollectionForm(formData);
  if (!parsed.ok) return parsed.state;

  try {
    const collection = await createCollection({
      ...parsed.value,
      isActive: formData.get("isActive") === "on",
      isFeatured: formData.get("isFeatured") === "on",
      imageUrl: null,
    }, await requireAdminActionSession());
    revalidateCollectionRoutes();
    return { status: "success", collectionId: collection.id };
  } catch (error) {
    return collectionActionError(error, "Koleksiyon oluşturulamadı.");
  }
}

// Burada mevcut koleksiyonun içerik alanlarıyla birlikte korunacak, değişecek veya kaldırılacak görsel URL'sini güncelliyorum.
export async function updateManualCollectionAction(
  id: string,
  _previousState: CollectionActionState,
  formData: FormData,
): Promise<CollectionActionState> {
  const parsed = parseCollectionForm(formData);
  if (!parsed.ok) return { ...parsed.state, collectionId: id };
  if (parsed.imageMode === "replace" && !isTrustedCollectionImageAsset(parsed.imageAsset, id)) {
    return {
      status: "error",
      collectionId: id,
      message: "Görsel kaynağı doğrulanamadı.",
      fieldErrors: { imageUrl: ["Görsel bu koleksiyonun Cloudinary klasörüne ait değil."] },
    };
  }

  try {
    const session = await requireAdminActionSession();
    const current = await getCollection(id, session);
    const imageUrl = parsed.imageMode === "remove"
      ? null
      : parsed.imageMode === "replace"
        ? parsed.imageAsset?.secureUrl || null
        : current.imageUrl;
    await updateCollection(id, { ...parsed.value, imageUrl }, session);
    revalidateCollectionRoutes(id);
    return { status: "success", collectionId: id };
  } catch (error) {
    return { ...collectionActionError(error, "Koleksiyon güncellenemedi."), collectionId: id };
  }
}

// Burada yeni oluşan koleksiyona Cloudinary görselini tam PUT sözleşmesiyle bağlıyorum.
export async function attachCollectionImageAction(id: string, asset: CloudinaryAsset): Promise<CollectionActionState> {
  if (!isTrustedCollectionImageAsset(asset, id)) {
    return { status: "partial", collectionId: id, message: "Koleksiyon oluşturuldu ancak görsel kaynağı doğrulanamadı." };
  }

  try {
    const session = await requireAdminActionSession();
    const collection = await getCollection(id, session);
    await updateCollection(id, {
      name: collection.name,
      url: collection.url,
      description: collection.description,
      displayOrder: collection.displayOrder,
      imageUrl: asset.secureUrl,
    }, session);
    revalidateCollectionRoutes(id);
    return { status: "success", collectionId: id };
  } catch (error) {
    const state = collectionActionError(error, "Koleksiyon oluşturuldu ancak görsel koleksiyona bağlanamadı.");
    return { ...state, status: "partial", collectionId: id };
  }
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

// Burada API hatasını form URL'sinde güvenle gösterilebilecek sınırlı bir hata koduna dönüştürüyorum.
function collectionErrorCode(error: unknown): string {
  if (!(error instanceof ApiError)) return "failed";
  if (error.problem.status === 401) return "session";
  if (error.problem.status === 403) return "forbidden";
  if (error.problem.status === 404) return "not-found";
  if (error.problem.status === 409) return "conflict";
  return "failed";
}

// Burada koleksiyonu silip ürünleri koruyarak yalnız katalog bağlantılarını kaldırıyorum.
export async function deleteCollectionAction(id: string): Promise<AdminMutationResult> {
  try {
    await deleteCollection(id, await requireAdminActionSession());
    revalidateCollectionRoutes(id);
    return { status: "success", message: "Koleksiyon silindi.", redirectHref: "/collections?deleted=1" };
  } catch (error) {
    return adminMutationError(error, "Koleksiyon silinemedi.", "Silme işlemi başka bir değişiklikle çakıştı. Sayfayı yenileyip tekrar deneyin.");
  }
}

// Burada API hatalarını formda veri kaybı oluşturmadan gösterilecek yapısal duruma dönüştürüyorum.
function collectionActionError(error: unknown, fallback: string): CollectionActionState {
  if (!(error instanceof ApiError)) return { status: "error", message: fallback };
  const message = error.problem.status === 401
    ? "Oturumunuz sona erdi. Form verinizi kaybetmeden yeniden giriş yapın."
    : error.problem.status === 403
      ? "Bu işlem için yönetici yetkiniz bulunmuyor."
      : error.problem.status === 404
        ? "Koleksiyon artık bulunamıyor. Listeyi yenileyin."
        : error.problem.status === 409
          ? "Bu bağlantı adresi başka bir koleksiyon tarafından kullanılıyor."
          : error.problem.detail || fallback;
  return { status: "error", message, fieldErrors: error.problem.errors, traceId: error.problem.traceId };
}

// Burada koleksiyon ve ürün ekranlarının değişiklikten sonra yetkili veriyi yeniden okumasını sağlıyorum.
function revalidateCollectionRoutes(id?: string): void {
  revalidatePath("/collections");
  revalidatePath("/products");
  if (id) revalidatePath(`/collections/${id}`);
}
