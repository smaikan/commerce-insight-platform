"use server";

import { randomUUID } from "node:crypto";
import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { adminMutationError } from "@/lib/admin/mutation-error";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  bulkUpdateProductVariants,
  createProduct,
  createProductImage,
  createProductVariant,
  deleteProduct,
  deleteProductImage,
  deleteProductVariant,
  getProduct,
  getProductImages,
  patchProductState,
  updateProduct,
  updateProductImage,
} from "@/modules/products/api";
import { parseProductForm } from "@/modules/products/form-data";
import { productActionError, remapProductVariantBulkError } from "@/modules/products/action-error";
import {
  isTrustedCloudinaryProductAsset,
  MAX_PRODUCT_IMAGES,
  type ProductMediaCommitInput,
  type ProductMediaCommitResult,
} from "@/modules/products/product-media";
import type { ProductActionState, ProductVariant } from "@/modules/products/types";
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

// Burada kayıtlı varyantı geçmişini koruyarak kaldırıp ürün detayının authoritative verisini geçersiz kılıyorum.
export async function deleteProductVariantAction(productId: string, variantId: string): Promise<AdminMutationResult> {
  try {
    await deleteProductVariant(variantId, await requireAdminActionSession());
    revalidatePath("/products");
    revalidatePath(`/products/${encodeURIComponent(productId)}`);
    return { status: "success", message: "Varyant üründen kaldırıldı.", refresh: true };
  } catch (error) {
    return adminMutationError(
      error,
      "Varyant silinemedi.",
      "Ürünün son kalan varyantı silinemez.",
    );
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
    return productActionError(error, { productId, completedOperations, failedOperation });
  }

  revalidatePath("/products");
  return { status: "success", productId, completionToken: randomUUID() };
}

// Burada ürün düzenlemesini ayrı backend işlemlerine bölüp olası kısmi başarıyı açıkça koruyorum.
export async function updateProductAction(
  previousState: ProductActionState,
  formData: FormData,
): Promise<ProductActionState> {
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return { ...authorizationError(error), savedVariantEditorState: previousState.savedVariantEditorState };
  }

  const parsed = parseProductForm(formData, "edit");
  if (!parsed.ok) {
    return {
      status: "error",
      message: parsed.message,
      fieldErrors: parsed.fieldErrors,
      savedVariantEditorState: previousState.savedVariantEditorState,
    };
  }

  const productId = parsed.value.productId;
  if (!productId) {
    return {
      status: "error",
      message: "Ürün kimliği bulunamadı.",
      savedVariantEditorState: previousState.savedVariantEditorState,
    };
  }

  const completedOperations: string[] = [];
  const operationFailures: ProductOperationFailure[] = [];
  let bulkUpdatedVariants: ProductVariant[] = [];
  let variantMutationAttempted = false;
  let failedOperation = "ürün güncellemesi";
  let conflictMessage: string | undefined;
  let conflictField: string | undefined;
  try {
    const variantEntries = [...parsed.value.variants.entries()];
    const isEnablingVariantMode = !parsed.value.originalHasVariants && parsed.value.hasVariants;

    // Burada bütün değişen mevcut varyantları SKU takaslarını da kapsayan tek atomik request'e dönüştürüyorum.
    const persistedEntries = variantEntries.filter(([, variant]) => Boolean(variant.id));
    if (persistedEntries.length > 0) {
      variantMutationAttempted = true;
      failedOperation = "varyantların toplu kaydı";
      conflictMessage = undefined;
      conflictField = undefined;
      const bulkFieldIndexes = persistedEntries.map(([variantIndex]) => parsed.value.variantFieldIndexes[variantIndex]);
      try {
        // Burada ürün formundan gelen mevcut stok değerine güvenmeyip yalnız stok defterinin authoritative bakiyesini koruyorum.
        const authoritativeProduct = await getProduct(productId, session);
        const authoritativeVariantsById = new Map(
          authoritativeProduct.variants.map((variant) => [variant.id, variant]),
        );
        bulkUpdatedVariants = await bulkUpdateProductVariants(productId, {
          variants: persistedEntries.map(([, variant]) => {
            if (!variant.id || !variant.expectedConcurrencyToken) {
              throw new Error("Persisted product variant concurrency data is missing.");
            }
            const authoritativeVariant = authoritativeVariantsById.get(variant.id);
            if (!authoritativeVariant) {
              throw new Error("Persisted product variant could not be found in the current product record.");
            }
            return {
              id: variant.id,
              name: variant.name,
              value: variant.value,
              sku: variant.sku,
              price: variant.price,
              stock: authoritativeVariant.stock,
              compareAtPrice: variant.compareAtPrice,
              barcode: variant.barcode,
              material: variant.material,
              isActive: variant.isActive,
              stockAdjustmentReason: null,
              expectedConcurrencyToken: variant.expectedConcurrencyToken,
            };
          }),
        }, session);
        completedOperations.push(`${bulkUpdatedVariants.length} varyantın atomik kaydı`);
      } catch (error) {
        operationFailures.push({
          error: remapProductVariantBulkError(error, bulkFieldIndexes),
          failedOperation,
        });
      }
    }

    // Burada yalnız yeni kombinasyonları tekil create sözleşmesiyle ekliyorum; mevcut varyantlar tekrar PUT edilmez.
    const persistNewVariant = async ([variantIndex, variant]: (typeof variantEntries)[number]) => {
      if (variant.id) return;
      variantMutationAttempted = true;
      failedOperation = `${variant.name}: ${variant.value} yeni varyantı`;
      conflictMessage = "Bu SKU başka bir varyantta kullanılıyor. Her varyant için benzersiz bir SKU girin.";
      conflictField = `variants.${parsed.value.variantFieldIndexes[variantIndex]}.sku`;
      try {
        await createProductVariant(productId, variant, session);
        completedOperations.push(failedOperation);
      } catch (error) {
        operationFailures.push({
          error,
          failedOperation,
          conflictMessage,
          conflictField,
        });
      }
    };

    if (parsed.value.originalHasVariants !== parsed.value.hasVariants) {
      variantMutationAttempted = true;
      failedOperation = "varyant modu";
      conflictMessage = undefined;
      conflictField = undefined;
      await patchProductState(productId, "has-variants", { hasVariants: parsed.value.hasVariants }, session);
      completedOperations.push(failedOperation);
    }

    for (const entry of variantEntries) {
      if (isEnablingVariantMode && entry[1].id) continue;
      await persistNewVariant(entry);
    }

    // Burada bağımsız ürün alanlarını varyantlardan sonra kaydediyorum; tekil varyant hataları bu işlemleri durdurmuyor.
    if (parsed.value.baseChanged) {
      failedOperation = "temel ürün bilgileri";
      conflictMessage = undefined;
      conflictField = undefined;
      await updateProduct(productId, parsed.value.base as Parameters<typeof updateProduct>[1], session);
      completedOperations.push(failedOperation);
    }

    if (parsed.value.originalStatus !== parsed.value.status) {
      failedOperation = "ürün durumu";
      conflictMessage = undefined;
      conflictField = undefined;
      await patchProductState(productId, "status", { status: parsed.value.status }, session);
      completedOperations.push(failedOperation);
    }
    if (parsed.value.originalIsFeatured !== parsed.value.isFeatured) {
      failedOperation = "öne çıkarma tercihi";
      conflictMessage = undefined;
      conflictField = undefined;
      await patchProductState(productId, "featured", { isFeatured: parsed.value.isFeatured }, session);
      completedOperations.push(failedOperation);
    }

    if (parsed.value.image) {
      failedOperation = "ürün görseli";
      conflictMessage = undefined;
      conflictField = undefined;
      await createProductImage(productId, parsed.value.image, session);
      completedOperations.push(failedOperation);
    }
  } catch (error) {
    operationFailures.push({ error, failedOperation, conflictMessage, conflictField });
  }

  let savedVariantEditorState;
  if (variantMutationAttempted || operationFailures.length > 0) {
    try {
      const savedProduct = await getProduct(productId, session);
      savedVariantEditorState = {
        mainSku: savedProduct.mainSku,
        hasVariants: savedProduct.hasVariants,
        variants: mergeAuthoritativeVariantUpdates(savedProduct.variants, bulkUpdatedVariants),
      };
    } catch {
      savedVariantEditorState = undefined;
    }
  }
  revalidatePath("/products");
  revalidatePath(`/products/${productId}`);
  if (operationFailures.length > 0) {
    return buildProductOperationFailureState({
      productId,
      completedOperations,
      operationFailures,
      currentRecordAvailable: Boolean(savedVariantEditorState),
      savedVariantEditorState: savedVariantEditorState || previousState.savedVariantEditorState,
    });
  }
  return { status: "success", productId, completionToken: randomUUID(), savedVariantEditorState };
}

// Burada bulk response'taki yeni concurrency tokenlarını sonraki submit için GET sonucuna öncelikli yazarım.
function mergeAuthoritativeVariantUpdates(
  variants: ProductVariant[],
  updates: ProductVariant[],
): ProductVariant[] {
  if (updates.length === 0) return variants;
  const updatesById = new Map(updates.map((variant) => [variant.id, variant]));
  return variants.map((variant) => updatesById.get(variant.id) || variant);
}

type ProductOperationFailure = {
  error: unknown;
  failedOperation: string;
  conflictMessage?: string;
  conflictField?: string;
};

type ProductOperationFailureStateInput = {
  productId: string;
  completedOperations: string[];
  operationFailures: ProductOperationFailure[];
  currentRecordAvailable: boolean;
  savedVariantEditorState?: ProductActionState["savedVariantEditorState"];
};

// Burada bağımsız işlemlerde biriken hataları tek bir kısmi kayıt sonucunda ve birleşik alan hatalarında topluyorum.
function buildProductOperationFailureState(input: ProductOperationFailureStateInput): ProductActionState {
  const failureStates = input.operationFailures.map((failure, index) => productActionError(failure.error, {
    productId: input.productId,
    completedOperations: index === 0 ? input.completedOperations : [],
    failedOperation: failure.failedOperation,
    currentRecordAvailable: input.currentRecordAvailable,
    conflictMessage: failure.conflictMessage,
    conflictField: failure.conflictField,
  }));
  const primaryFailure = failureStates[0];
  const fieldErrors = mergeProductFieldErrors(failureStates);
  const failedOperations = input.operationFailures.map((failure) => failure.failedOperation);
  const additionalFailureMessage = failedOperations.length > 1
    ? ` Toplam ${failedOperations.length} işlem tamamlanamadı: ${failedOperations.join(", ")}.`
    : "";

  return {
    ...primaryFailure,
    status: input.completedOperations.length > 0 ? "partial" : "error",
    message: `${primaryFailure.message || "Ürün güncellemesi tamamlanamadı."}${additionalFailureMessage}`,
    traceId: failureStates.find((state) => state.traceId)?.traceId,
    reloadHref: failureStates.find((state) => state.reloadHref)?.reloadHref,
    fieldErrors,
    completedOperations: input.completedOperations,
    failedOperations,
    savedVariantEditorState: input.savedVariantEditorState,
  };
}

// Burada farklı varyantların alan hatalarını aynı form sonucunda kayıp olmadan birleştiriyorum.
function mergeProductFieldErrors(states: ProductActionState[]): Record<string, string[]> | undefined {
  const entries = states.flatMap((state) => Object.entries(state.fieldErrors || {}));
  if (entries.length === 0) return undefined;

  return entries.reduce<Record<string, string[]>>((merged, [field, messages]) => {
    merged[field] = [...(merged[field] || []), ...messages];
    return merged;
  }, {});
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
      updatedExistingImageIds: [],
    };
  }

  const validationMessage = validateMediaCommitInput(input);
  if (validationMessage) {
    return {
      status: "error",
      productId: input.productId,
      message: validationMessage,
      committedClientKeys: [],
      updatedExistingImageIds: [],
    };
  }

  const committedClientKeys: string[] = [];
  const updatedExistingImageIds: string[] = [];
  try {
    const currentImages = await getProductImages(input.productId, session);
    if (currentImages.items.length + input.newImages.length > MAX_PRODUCT_IMAGES) {
      throw new Error(`Bir üründe en fazla ${MAX_PRODUCT_IMAGES} görsel bulunabilir.`);
    }

    const currentById = new Map(currentImages.items.map((image) => [image.id, image]));
    if (input.existingImages.some((image) => !currentById.has(image.id))) {
      throw new Error("Görsel sırası bu ürüne ait olmayan bir kayıt içeriyor.");
    }
    if (input.mainExistingImageId && !currentById.has(input.mainExistingImageId)) {
      throw new Error("Ana görsel seçimi bu ürüne ait değil.");
    }

    // Burada kayıtlı görsellerin yalnız değişen sırasını ve seçilen ana görsel niyetini mevcut PUT sözleşmesiyle kaydediyorum.
    for (const orderedImage of input.existingImages) {
      const current = currentById.get(orderedImage.id);
      if (!current) continue;
      const shouldBecomeMain = input.mainExistingImageId === current.id;
      if (current.displayOrder === orderedImage.displayOrder && (!shouldBecomeMain || current.isMain)) continue;
      await updateProductImage(current.id, {
        imageUrl: current.imageUrl,
        altText: current.altText,
        displayOrder: orderedImage.displayOrder,
        isMain: shouldBecomeMain,
      }, session);
      updatedExistingImageIds.push(current.id);
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
      status: committedClientKeys.length > 0 || updatedExistingImageIds.length > 0 ? "partial" : "error",
      productId: input.productId,
      message: apiMessage,
      traceId: error instanceof ApiError ? error.problem.traceId : undefined,
      committedClientKeys,
      updatedExistingImageIds,
    };
  }

  revalidatePath("/products");
  revalidatePath(`/products/${input.productId}`);
  return {
    status: "success",
    productId: input.productId,
    committedClientKeys,
    updatedExistingImageIds,
  };
}

// Burada istemciden gelen medya kaydının sınırlarını, tek ana seçim kuralını ve Cloudinary kaynağını yeniden doğruluyorum.
function validateMediaCommitInput(input: ProductMediaCommitInput): string | null {
  if (!/^P[0-9A-Z]{5,7}$/.test(input.productId)) return "Geçerli bir ürün kimliği bulunamadı.";
  if (input.newImages.length > MAX_PRODUCT_IMAGES) return `En fazla ${MAX_PRODUCT_IMAGES} görsel eklenebilir.`;
  if (input.existingImages.length > MAX_PRODUCT_IMAGES) return `En fazla ${MAX_PRODUCT_IMAGES} kayıtlı görsel sıralanabilir.`;
  if (new Set(input.existingImages.map((image) => image.id)).size !== input.existingImages.length) return "Tekrarlanan kayıtlı görsel sırası gönderilemez.";
  if (input.existingImages.some((image) => !Number.isInteger(image.displayOrder) || image.displayOrder < 0)) return "Kayıtlı görsel sırası geçersiz.";
  if (new Set(input.newImages.map((image) => image.clientKey)).size !== input.newImages.length) return "Tekrarlanan görsel kaydı gönderilemez.";
  if (input.newImages.some((image) => !Number.isInteger(image.displayOrder) || image.displayOrder < 0)) return "Görsel sırası geçersiz.";
  const allDisplayOrders = [
    ...input.existingImages.map((image) => image.displayOrder),
    ...input.newImages.map((image) => image.displayOrder),
  ];
  if (new Set(allDisplayOrders).size !== allDisplayOrders.length) return "Görsel sıraları birbirinden farklı olmalıdır.";

  const newMainCount = input.newImages.filter((image) => image.isMain).length;
  if (newMainCount > 1 || (newMainCount === 1 && input.mainExistingImageId)) return "Yalnızca bir ana görsel seçilebilir.";

  const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim();
  if (input.newImages.length > 0 && !cloudName) return "Görsel yükleme hizmeti yapılandırılmamış.";
  if (cloudName && input.newImages.some((image) => !isTrustedCloudinaryProductAsset(image, input.productId, cloudName))) {
    return "Görsel kaynağı veya ürün klasörü doğrulanamadı.";
  }
  return null;
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
