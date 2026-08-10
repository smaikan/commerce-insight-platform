"use server";

import { revalidatePath } from "next/cache";
import { isTrustedCloudinaryAsset, type CloudinaryAsset } from "@/lib/cloudinary/browser-upload";
import { adminMutationError } from "@/lib/admin/mutation-error";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createBrand, deleteBrand, getBrand, setBrandActivation, updateBrand } from "@/modules/brands/api";
import type { BrandActionState, CreateBrandInput, UpdateBrandInput } from "@/modules/brands/types";

// Burada görsel yüklemesinden önce marka kaydını oluşturup istemciye kalıcı kimliğini döndürüyorum.
export async function createBrandAction(
  previousState: BrandActionState,
  formData: FormData,
): Promise<BrandActionState> {
  void previousState;
  const parsed = parseBrandForm(formData, true);
  if (!parsed.ok) return parsed.state;

  try {
    const brand = await createBrand({ ...parsed.value as CreateBrandInput, imageUrl: null }, await requireAdminActionSession());
    revalidateBrandRoutes();
    return { status: "created", brandId: brand.id, message: "Marka oluşturuldu." };
  } catch (error) {
    return brandActionError(error, "Marka oluşturulamadı");
  }
}

// Burada mevcut markanın metin ve görsel alanlarını PUT sözleşmesine göre birlikte güncelliyorum.
export async function updateBrandAction(
  id: string,
  previousState: BrandActionState,
  formData: FormData,
): Promise<BrandActionState> {
  void previousState;
  const parsed = parseBrandForm(formData, false, id);
  if (!parsed.ok) return parsed.state;

  try {
    const session = await requireAdminActionSession();
    const imageIntent = text(formData, "imageIntent") || "keep";
    const current = imageIntent === "keep" ? await getBrand(id, session) : null;
    const imageUrl = imageIntent === "keep"
      ? current?.imageUrl || null
      : imageIntent === "remove"
        ? null
        : (parsed.value as UpdateBrandInput).imageUrl || null;
    await updateBrand(id, { ...parsed.value as UpdateBrandInput, imageUrl }, session);
    revalidateBrandRoutes(id);
    return { status: "success", redirectHref: "/brands?updated=1", message: "Marka güncellendi." };
  } catch (error) {
    return brandActionError(error, "Marka güncellenemedi");
  }
}

// Burada yeni oluşturulan markaya Cloudinary görselini güncel marka bilgilerini koruyarak bağlıyorum.
export async function attachBrandImageAction(id: string, asset: CloudinaryAsset): Promise<BrandActionState> {
  if (!isTrustedUploadedBrandImage(asset, id)) {
    return { status: "error", message: "Yüklenen görsel doğrulanamadı. Lütfen tekrar deneyin." };
  }

  try {
    const session = await requireAdminActionSession();
    const brand = await getBrand(id, session);
    await updateBrand(id, {
      name: brand.name,
      url: brand.url || null,
      description: brand.description || null,
      imageUrl: asset.secureUrl,
    }, session);
    revalidateBrandRoutes(id);
    return { status: "success", redirectHref: "/brands?created=1", message: "Marka ve görseli oluşturuldu." };
  } catch (error) {
    return brandActionError(error, "Marka oluşturuldu ancak görsel bağlanamadı");
  }
}

// Burada marka aktifliğini liste satırındaki bağımsız eylemden güvenli biçimde değiştiriyorum.
export async function setBrandActivationAction(
  id: string,
  isActive: boolean,
  previousState: BrandActionState,
): Promise<BrandActionState> {
  void previousState;
  try {
    await setBrandActivation(id, isActive, await requireAdminActionSession());
    revalidateBrandRoutes(id);
    return { status: "success", message: isActive ? "Marka etkinleştirildi." : "Marka pasifleştirildi." };
  } catch (error) {
    return brandActionError(error, "Marka durumu değiştirilemedi");
  }
}

// Burada markayı silip bağlı ürünlerin markasız kalmasını backend ilişkisine bırakıyorum.
export async function deleteBrandAction(id: string): Promise<AdminMutationResult> {
  try {
    await deleteBrand(id, await requireAdminActionSession());
    revalidateBrandRoutes(id);
    return { status: "success", message: "Marka silindi.", redirectHref: "/brands?deleted=1" };
  } catch (error) {
    return adminMutationError(error, "Marka silinemedi.", "Silme işlemi başka bir değişiklikle çakıştı. Sayfayı yenileyip tekrar deneyin.");
  }
}

// Burada marka formunu backend uzunluk ve nullable görsel sözleşmesine göre doğruluyorum.
function parseBrandForm(
  formData: FormData,
  create: boolean,
  brandId?: string,
): { ok: true; value: CreateBrandInput | UpdateBrandInput } | { ok: false; state: BrandActionState } {
  const name = text(formData, "name");
  const url = text(formData, "url");
  const description = text(formData, "description");
  const imageUrl = text(formData, "imageUrl");
  const imagePublicId = text(formData, "imagePublicId");
  const imageResourceType = text(formData, "imageResourceType");
  const imageIntent = text(formData, "imageIntent") || "keep";
  const fieldErrors: Record<string, string[]> = {};

  if (!name || name.length > 150) fieldErrors.name = ["Marka adı 1–150 karakter olmalıdır."];
  if (url.length > 200) fieldErrors.url = ["URL değeri en fazla 200 karakter olabilir."];
  if (description.length > 1000) fieldErrors.description = ["Açıklama en fazla 1.000 karakter olabilir."];
  if (imageUrl.length > 500) fieldErrors.imageUrl = ["Görsel bağlantısı en fazla 500 karakter olabilir."];
  const imageAsset: CloudinaryAsset = {
    secureUrl: imageUrl,
    publicId: imagePublicId,
    resourceType: imageResourceType === "video" ? "video" : "image",
  };
  if (!create && imageIntent === "replace" && (!brandId || !isTrustedUploadedBrandImage(imageAsset, brandId))) {
    fieldErrors.imageUrl = ["Yüklenen görsel doğrulanamadı. Lütfen görseli yeniden seçin."];
  }
  if (Object.keys(fieldErrors).length > 0) {
    return { ok: false, state: { status: "error", message: "İşaretli alanları kontrol edin.", fieldErrors } };
  }

  const base = { name, url: url || null, description: description || null, imageUrl: imageUrl || null };
  return create
    ? { ok: true, value: { ...base, isActive: formData.get("isActive") === "on" } }
    : { ok: true, value: base };
}

// Burada tarayıcıdan gelen görsel URL'sinin bu marka klasörüne ve yapılandırılmış Cloudinary hesabına ait olduğunu doğruluyorum.
function isTrustedUploadedBrandImage(asset: CloudinaryAsset, brandId: string): boolean {
  return isTrustedCloudinaryAsset(asset, {
    folder: `brands/${brandId}`,
    allowedResourceTypes: ["image"],
  });
}

// Burada API hatasını taslak veriyi koruyan güvenli form sonucuna dönüştürüyorum.
function brandActionError(error: unknown, prefix: string): BrandActionState {
  if (!(error instanceof ApiError)) return { status: "error", message: `${prefix}. Lütfen tekrar deneyin.` };
  const message = error.problem.status === 401
    ? "Oturumunuz sona erdi. Yeniden giriş yaptıktan sonra tekrar deneyin."
    : error.problem.status === 403
      ? "Bu işlem için yönetici yetkiniz bulunmuyor."
      : error.problem.status === 404
        ? "Marka artık bulunamıyor. Listeyi yenileyin."
        : error.problem.status === 409
          ? "Bu marka bilgileri başka bir kayıtla çakışıyor. Güncel veriyi kontrol edin."
          : `${prefix}: ${error.problem.detail || error.problem.title}`;
  return { status: "error", message, traceId: error.problem.traceId, fieldErrors: error.problem.errors };
}

// Burada marka ve ürün ekranlarının mutation sonrasında yetkili veriyi yeniden okumasını sağlıyorum.
function revalidateBrandRoutes(id?: string): void {
  revalidatePath("/brands");
  if (id) revalidatePath(`/brands/${encodeURIComponent(id)}`);
  revalidatePath("/products");
}

// Burada tekil form metnini boşlukları ayıklanmış biçimde alıyorum.
function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}
