"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import {
  createProduct,
  createProductImage,
  createProductVariant,
  getProduct,
  patchProductState,
  updateProduct,
  updateProductVariant,
} from "@/modules/products/api";
import { parseProductForm } from "@/modules/products/form-data";
import type { ProductActionState } from "@/modules/products/types";

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
  redirect(`/products/${encodeURIComponent(productId)}?created=1`);
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
  redirect(`/products/${encodeURIComponent(productId)}?saved=1`);
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
