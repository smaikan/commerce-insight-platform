"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { createBulkStockMovements } from "@/modules/inventory/api";
import { supportsManualStockMovement } from "@/modules/inventory/stock-movement-rules";
import type { BulkStockMovement, StockMovementActionState } from "@/modules/inventory/types";

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

// Burada toplu stok hareketlerini Admin oturumu altında tek atomik API işlemine dönüştürüyorum.
export async function createBulkStockMovementsAction(
  _previousState: StockMovementActionState,
  formData: FormData,
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
    const result = await createBulkStockMovements(parsed.movements, session);
    revalidatePath("/inventory/stock-movements");
    return { status: "success", message: `${result.movementCount} stok hareketi atomik olarak kaydedildi.`, movementCount: result.movementCount };
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

// Burada tarayıcıdan gelen taslağı UUID, tür, yön, miktar ve açıklama sınırlarıyla doğruluyorum.
function parseBulkMovements(raw: FormDataEntryValue | null): { ok: true; movements: BulkStockMovement[] } | { ok: false; state: StockMovementActionState } {
  if (typeof raw !== "string") return { ok: false, state: { status: "error", message: "Hareket satırları bulunamadı." } };

  let value: unknown;
  try {
    value = JSON.parse(raw);
  } catch {
    return { ok: false, state: { status: "error", message: "Hareket satırları geçerli değil." } };
  }
  if (!Array.isArray(value) || value.length === 0 || value.length > 500) {
    return { ok: false, state: { status: "error", message: "Bir ile 500 arasında hareket satırı girin." } };
  }

  const movements: BulkStockMovement[] = [];
  for (const [index, item] of value.entries()) {
    if (!isRecord(item)) return rowError(index, "Satır biçimi geçerli değil.");
    const productVariantId = typeof item.productVariantId === "string" ? item.productVariantId.trim() : "";
    const type = Number(item.type);
    const direction = Number(item.direction);
    const quantity = Number(item.quantity);
    const reason = typeof item.reason === "string" ? item.reason.trim() : "";
    if (!uuidPattern.test(productVariantId)) return rowError(index, "Varyant kimliği geçerli bir UUID olmalı.");
    if (!Number.isInteger(quantity) || quantity <= 0 || quantity > 2_147_483_647) return rowError(index, "Miktar pozitif tam sayı olmalı.");
    if (!supportsManualStockMovement(type, direction)) return rowError(index, "Hareket türü ve yön birbiriyle uyumlu değil.");
    if (reason.length > 500) return rowError(index, "Açıklama en fazla 500 karakter olabilir.");
    movements.push({ productVariantId, type, quantityDelta: direction === 1 ? quantity : -quantity, reason: reason || null });
  }
  return { ok: true, movements };
}

// Burada satır bazlı doğrulama hatasını formda anlaşılır tek mesaj olarak hazırlıyorum.
function rowError(index: number, message: string): { ok: false; state: StockMovementActionState } {
  return { ok: false, state: { status: "error", message: `${index + 1}. satır: ${message}` } };
}

// Burada JSON içeriğinin nesne olup olmadığını güvenli biçimde ayırıyorum.
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
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
