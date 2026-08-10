"use server";

import { randomUUID } from "node:crypto";
import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { adminMutationError } from "@/lib/admin/mutation-error";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  createProduct,
  createProductImage,
  createProductVariant,
  deleteProduct,
  deleteProductImage,
  getProduct,
  getProductImages,
  patchProductState,
  updateProduct,
  updateProductImage,
  updateProductVariant,
} from "@/modules/products/api";
import { parseProductForm } from "@/modules/products/form-data";
import {
  isTrustedCloudinaryProductAsset,
  MAX_PRODUCT_IMAGES,
  type ProductMediaCommitInput,
  type ProductMediaCommitResult,
} from "@/modules/products/product-media";
import type { ProductActionState } from "@/modules/products/types";
import type { ProductStatus } from "@/modules/products/types";

// Burada ürün listesindeki hızlı durum geçişini yalnız belgelenen status endpoint'iyle uygularım.
export async function setProductListStatusAction(productId: string, status: ProductStatus): Promise<AdminMutationResult> {
  try {
    const session = await requireAdminActionSession();
    await patchProductState(productId, "status", { status }, session);
    revalidatePath("/products");
    revalidatePath(`/products/${encodeURIComponent(productId)}`);
    return { status: "success", message: status === 1 ? "Ürün aktifleştirildi." : "Ürün taslağa alındı." };
  } catch (error) {
    return adminMutationError(error, "Ürün durumu değiştirilemedi.", "Ürün durumu başka bir işlemle çakıştı. Sayfayı yenileyip tekrar deneyin.");
  }
}

// Burada ürünü operasyon geçmişini koruyarak arşive taşıyan silme işlemini çalıştırıyorum.
export async function deleteProductAction(productId: string): Promise<AdminMutationResult> {
  try {
    await deleteProduct(productId, await requireAdminActionSession());
    revalidatePath("/products");
    return { status: "success", message: "Ürün arşive taşındı.", redirectHref: "/products?deleted=1" };
  } catch (error) {
    return adminMutationError(
      error,
      "Ürün silinemedi.",
      "Silme işlemi başka bir değişiklikle çakıştı. Sayfayı yenileyip tekrar deneyin.",
    );
  }
}

// Burada kayıtlı ürün görselini silip yeni ana görsel seçimini backend'e bırakıyorum.
export async function deleteProductImageAction(productId: string, imageId: string): Promise<AdminMutationResult> {
  try {
    await deleteProductImage(imageId, await requireAdminActionSession());
    revalidatePath("/products");
    revalidatePath(`/products/${encodeURIComponent(productId)}`);
    return { status: "success", message: "Görsel üründen kaldırıldı." };
  } catch (error) {
    return adminMutationError(error, "Görsel silinemedi.", "Görsel başka bir işlem tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.");
  }
}

// Burada yeni ürün, atomik varyantlar ve opsiyonel ayrı görsel işlemini güvenli sırada çalıştırıyorum.
export async function createProductAction(
  _previousState: ProductActionState,
  formData: FormData,
): Promise<ProductActionState> {
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return authorizationError(error);
  }

  const parsed = parseProductForm(formData, "create");
  if (!parsed.ok) return { status: "error", message: parsed.message, fieldErrors: parsed.fieldErrors };

  let productId: string | undefined;
  const completedOperations: string[] = [];
  let failedOperation = "ürün oluşturma";
  try {
    const product = await createProduct(parsed.value.base as Parameters<typeof createProduct>[0], session);
    productId = product.id;
    completedOperations.push("ürün kaydı");

    if (parsed.value.image) {
      failedOperation = "ürün görseli";
      await createProductImage(product.id, parsed.value.image, session);
      completedOperations.push("ürün görseli");
    }
  } catch (error) {
    return actionError(error, { productId, completedOperations, failedOperation });
  }

  revalidatePath("/products");
  return { status: "success", productId, completionToken: randomUUID() };
}

// Burada ürün düzenlemesini ayrı backend işlemlerine bölüp olası kısmi başarıyı açıkça koruyorum.
export async function updateProductAction(
  _previousState: ProductActionState,
  formData: FormData,
): Promise<ProductActionState> {
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return authorizationError(error);
  }

  const parsed = parseProductForm(formData, "edit");
  if (!parsed.ok) return { status: "error", message: parsed.message, fieldErrors: parsed.fieldErrors };

  const productId = parsed.value.productId;
  if (!productId) {
    return { status: "error", message: "Ürün kimliği bulunamadı." };
  }

  const completedOperations: string[] = [];
  let failedOperation = "ürün güncellemesi";
  try {
    if (parsed.value.baseChanged) {
      failedOperation = "temel ürün bilgileri";
      await updateProduct(productId, parsed.value.base as Parameters<typeof updateProduct>[1], session);
      completedOperations.push(failedOperation);
    }

    if (parsed.value.originalStatus !== parsed.value.status) {
      failedOperation = "ürün durumu";
      await patchProductState(productId, "status", { status: parsed.value.status }, session);
      completedOperations.push(failedOperation);
    }
    if (parsed.value.originalIsFeatured !== parsed.value.isFeatured) {
      failedOperation = "öne çıkarma tercihi";
      await patchProductState(productId, "featured", { isFeatured: parsed.value.isFeatured }, session);
      completedOperations.push(failedOperation);
    }
    if (parsed.value.originalHasVariants !== parsed.value.hasVariants) {
      failedOperation = "varyant modu";
      await patchProductState(productId, "has-variants", { hasVariants: parsed.value.hasVariants }, session);
      completedOperations.push(failedOperation);
    }

    for (const variant of parsed.value.variants) {
      failedOperation = variant.id
        ? `${variant.name}: ${variant.value} varyantı`
        : `${variant.name}: ${variant.value} yeni varyantı`;
      if (variant.id) {
        await updateProductVariant({ ...variant, id: variant.id }, session);
      } else {
        await createProductVariant(productId, variant, session);
      }
      completedOperations.push(failedOperation);
    }

    if (parsed.value.image) {
      failedOperation = "ürün görseli";
      await createProductImage(productId, parsed.value.image, session);
      completedOperations.push(failedOperation);
    }
  } catch (error) {
    let currentRecordAvailable = false;
    if (error instanceof ApiError && error.problem.status === 409) {
      try {
        await getProduct(productId, session);
        currentRecordAvailable = true;
      } catch {
        currentRecordAvailable = false;
      }
    }
    return actionError(error, { productId, completedOperations, failedOperation, currentRecordAvailable });
  }

  revalidatePath("/products");
  revalidatePath(`/products/${productId}`);
  return { status: "success", productId, completionToken: randomUUID() };
}

// Burada Cloudinary'ye yüklenmiş görselleri yetkili API üzerinden ürüne bağlayıp tek ana görsel kuralını backend'e bırakarak uyguluyorum.
export async function commitProductMediaAction(input: ProductMediaCommitInput): Promise<ProductMediaCommitResult> {
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    const authorization = authorizationError(error);
    return {
      status: "error",
      productId: input.productId,
      message: authorization.message,
      traceId: authorization.traceId,
      committedClientKeys: [],
      existingMainUpdated: false,
    };
  }

  const validationMessage = validateMediaCommitInput(input);
  if (validationMessage) {
    return {
      status: "error",
      productId: input.productId,
      message: validationMessage,
      committedClientKeys: [],
      existingMainUpdated: false,
    };
  }

  const committedClientKeys: string[] = [];
  let existingMainUpdated = false;
  try {
    const currentImages = await getProductImages(input.productId, session);
    if (currentImages.items.length + input.newImages.length > MAX_PRODUCT_IMAGES) {
      throw new Error(`Bir üründe en fazla ${MAX_PRODUCT_IMAGES} görsel bulunabilir.`);
    }

    // Burada kayıtlı bir görsel seçildiyse yalnız onu true yapıyor, diğer görselleri false olarak göndermiyorum.
    if (input.mainExistingImageId) {
      const selected = currentImages.items.find((image) => image.id === input.mainExistingImageId);
      if (!selected) throw new Error("Ana görsel seçimi bu ürüne ait değil.");
      if (!selected.isMain) {
        await updateProductImage(selected.id, {
          imageUrl: selected.imageUrl,
          altText: selected.altText,
          displayOrder: selected.displayOrder,
          isMain: true,
        }, session);
      }
      existingMainUpdated = true;
    }

    // Burada yeni seçilen ana görseli önce kaydederek sonraki başarısızlıklarda bile ana seçim niyetini koruyorum.
    const orderedImages = [...input.newImages].sort((left, right) => Number(right.isMain) - Number(left.isMain) || left.displayOrder - right.displayOrder);
    for (const image of orderedImages) {
      await createProductImage(input.productId, {
        imageUrl: image.imageUrl,
        altText: null,
        displayOrder: image.displayOrder,
        isMain: image.isMain,
      }, session);
      committedClientKeys.push(image.clientKey);
    }
  } catch (error) {
    const apiMessage = error instanceof ApiError
      ? error.problem.detail || error.problem.title
      : error instanceof Error ? error.message : "Görseller ürüne bağlanamadı.";
    return {
      status: committedClientKeys.length > 0 || existingMainUpdated ? "partial" : "error",
      productId: input.productId,
      message: apiMessage,
      traceId: error instanceof ApiError ? error.problem.traceId : undefined,
      committedClientKeys,
      existingMainUpdated,
    };
  }

  revalidatePath("/products");
  revalidatePath(`/products/${input.productId}`);
  return {
    status: "success",
    productId: input.productId,
    committedClientKeys,
    existingMainUpdated,
  };
}

// Burada istemciden gelen medya kaydının sınırlarını, tek ana seçim kuralını ve Cloudinary kaynağını yeniden doğruluyorum.
function validateMediaCommitInput(input: ProductMediaCommitInput): string | null {
  if (!/^P[0-9A-Z]{5,7}$/.test(input.productId)) return "Geçerli bir ürün kimliği bulunamadı.";
  if (input.newImages.length > MAX_PRODUCT_IMAGES) return `En fazla ${MAX_PRODUCT_IMAGES} görsel eklenebilir.`;
  if (new Set(input.newImages.map((image) => image.clientKey)).size !== input.newImages.length) return "Tekrarlanan görsel kaydı gönderilemez.";
  if (input.newImages.some((image) => !Number.isInteger(image.displayOrder) || image.displayOrder < 0)) return "Görsel sırası geçersiz.";

  const newMainCount = input.newImages.filter((image) => image.isMain).length;
  if (newMainCount > 1 || (newMainCount === 1 && input.mainExistingImageId)) return "Yalnızca bir ana görsel seçilebilir.";

  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  if (input.newImages.length > 0 && !cloudName) return "Görsel yükleme hizmeti yapılandırılmamış.";
  if (cloudName && input.newImages.some((image) => !isTrustedCloudinaryProductAsset(image, input.productId, cloudName))) {
    return "Görsel kaynağı veya ürün klasörü doğrulanamadı.";
  }
  return null;
}

// Burada hata mesajını hangi işlemlerin tamamlandığı ve hangi aşamanın durduğu bilgisiyle taşıyorum.
type ActionErrorContext = {
  productId?: string;
  completedOperations: string[];
  failedOperation: string;
  currentRecordAvailable?: boolean;
};

// Burada tamamlanan ve çakışan aşamaları ayırarak kullanıcıya güvenli bir sonraki adım sunuyorum.
function actionError(error: unknown, context: ActionErrorContext): ProductActionState {
  const partiallySaved = context.completedOperations.length > 0;
  if (error instanceof ApiError) {
    if (error.problem.status === 409) {
      const completed = partiallySaved ? `${summarizeOperations(context.completedOperations)} kaydedildi; ancak ` : "";
      const refreshMessage = context.currentRecordAvailable
        ? " Sunucudaki güncel kayıt doğrulandı; formdaki değerleriniz korunuyor."
        : " Güncel kayıt şu anda yeniden okunamadı; formdaki değerleriniz korunuyor.";
      return {
        status: partiallySaved ? "partial" : "error",
        message: `${completed}${context.failedOperation} başka bir işlemle çakıştığı için tamamlanamadı.${refreshMessage}`,
        traceId: error.problem.traceId,
        productId: context.productId,
        reloadHref: context.productId && context.currentRecordAvailable
          ? `/products/${encodeURIComponent(context.productId)}?reload=1`
          : undefined,
      };
    }

    return {
      status: partiallySaved ? "partial" : "error",
      message: partiallySaved
        ? `${summarizeOperations(context.completedOperations)} kaydedildi; ancak ${context.failedOperation} tamamlanamadı: ${error.problem.detail || error.problem.title}`
        : `${context.failedOperation} tamamlanamadı: ${error.problem.detail || error.problem.title}`,
      traceId: error.problem.traceId,
      productId: context.productId,
      fieldErrors: error.problem.errors,
    };
  }

  return {
    status: partiallySaved ? "partial" : "error",
    message: partiallySaved
      ? `${summarizeOperations(context.completedOperations)} kaydedildi; ancak ${context.failedOperation} beklenmeyen bir nedenle tamamlanamadı.`
      : `${context.failedOperation} beklenmeyen bir nedenle tamamlanamadı.`,
    productId: context.productId,
  };
}

// Burada uzun varyant listelerinde hata mesajını okunabilir bir tamamlanan işlem özetiyle sınırlandırıyorum.
function summarizeOperations(operations: string[]): string {
  if (operations.length <= 3) return operations.join(", ");
  return `${operations.slice(0, 2).join(", ")} ve ${operations.length - 2} işlem daha`;
}

// Burada doğrudan Server Action çağrısındaki 401 ve 403 sonuçlarını mutation başlatmadan güvenli form durumuna dönüştürüyorum.
function authorizationError(error: unknown): ProductActionState {
  if (error instanceof ApiError) {
    return {
      status: "error",
      message: error.problem.status === 403
        ? "Bu işlem yalnızca aktif yönetici hesaplarına açıktır."
        : error.problem.status === 401
          ? "Oturumunuz sona erdi. Form verinizi kaybetmeden yeniden giriş yapın."
          : "Yönetici oturumu şu anda doğrulanamadı. Lütfen tekrar deneyin.",
      traceId: error.problem.traceId,
    };
  }
  return { status: "error", message: "Yönetici oturumu doğrulanamadı." };
}
