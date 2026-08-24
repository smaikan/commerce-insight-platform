"use server";

import { randomUUID } from "node:crypto";
import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createBulkStockMovements } from "@/modules/inventory/api";
import { parseBulkMovements } from "@/modules/inventory/stock-movement-form-data";
import type { StockMovementActionState } from "@/modules/inventory/types";
import { getProduct } from "@/modules/products/api";

// Burada toplu stok hareketlerini Admin oturumu altında tek atomik API işlemine dönüştürüyorum.
export async function createBulkStockMovementsAction(
  _previousState: StockMovementActionState,
  formData: FormData,
): Promise<StockMovementActionState> {
  return createStockMovements(formData);
}

// Burada ürün düzenleme ekranındaki aynı stok hareketi akışından sonra ilgili ürün detayını da yeniliyorum.
export async function createProductStockMovementsAction(
  productId: string,
  _previousState: StockMovementActionState,
  formData: FormData,
): Promise<StockMovementActionState> {
  return createStockMovements(formData, productId);
}

// Burada bağımsız ve ürün bağlamlı formların yetki, doğrulama ve atomik API davranışını tek sınırda tutuyorum.
async function createStockMovements(
  formData: FormData,
  productId?: string,
): Promise<StockMovementActionState> {
  let session;
  try {
    session = await requireAdminActionSession();
  } catch (error) {
    return authorizationError(error);
  }

  const parsed = parseBulkMovements(formData.get("movements"));
  if (!parsed.ok) return parsed.state;

  try {
    if (productId) {
      const product = await getProduct(productId, session);
      const allowedSkus = new Set(product.variants.map((variant) => variant.sku));
      const invalidIndex = parsed.movements.findIndex((movement) => !allowedSkus.has(movement.productVariantSku));
      if (invalidIndex >= 0) {
        const message = "Seçilen varyant bu ürüne ait değil. Güncel ürün verisini yeniden yükleyin.";
        return {
          status: "error",
          message,
          fieldErrors: { [`movements[${invalidIndex}].productVariantSku`]: [message] },
        };
      }
    }
    const result = await createBulkStockMovements(parsed.movements, session);
    revalidatePath("/inventory/stock-movements");
    if (productId) revalidatePath(`/products/${encodeURIComponent(productId)}`);
    return {
      status: "success",
      message: `${result.movementCount} stok hareketi atomik olarak kaydedildi.`,
      movementCount: result.movementCount,
      completionToken: randomUUID(),
    };
  } catch (error) {
    if (error instanceof ApiError) {
      return {
        status: "error",
        message: error.problem.status === 409
          ? "Stok başka bir işlemle değişti. Form korunuyor; güncel durumu kontrol edip tekrar deneyin."
          : `Stok hareketleri kaydedilemedi: ${error.problem.detail || error.problem.title}`,
        traceId: error.problem.traceId,
        fieldErrors: error.problem.errors,
      };
    }
    return { status: "error", message: "Stok hareketleri beklenmeyen bir nedenle kaydedilemedi." };
  }
}

// Burada action seviyesindeki 401 ve 403 sonuçlarını formun koruyabileceği güvenli mesaja dönüştürüyorum.
function authorizationError(error: unknown): StockMovementActionState {
  if (error instanceof ApiError) {
    return {
      status: "error",
      message: error.problem.status === 403 ? "Bu işlem yalnızca aktif yönetici hesaplarına açıktır." : "Oturumunuz doğrulanamadı. Form verinizi kaybetmeden yeniden giriş yapın.",
      traceId: error.problem.traceId,
    };
  }
  return { status: "error", message: "Yönetici oturumu doğrulanamadı." };
}
