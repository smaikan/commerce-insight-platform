import { ApiError } from "../../lib/api/problem";
import type { ProductActionState } from "./types";

export type ProductActionErrorContext = {
  productId?: string;
  completedOperations: string[];
  failedOperation: string;
  currentRecordAvailable?: boolean;
  conflictMessage?: string;
  conflictField?: string;
};

// Burada concurrency ile iş kuralı/benzersizlik çakışmasını ProblemDetails code değerine göre ayırıyorum.
export function productActionError(error: unknown, context: ProductActionErrorContext): ProductActionState {
  const partiallySaved = context.completedOperations.length > 0;
  if (error instanceof ApiError) {
    if (error.problem.status === 409 && error.problem.code === "concurrency_conflict") {
      const completed = partiallySaved ? `${summarizeOperations(context.completedOperations)} kaydedildi; ancak ` : "";
      const refreshMessage = context.currentRecordAvailable
        ? " Sunucudaki güncel kayıt doğrulandı; formdaki değerleriniz korunuyor."
        : " Güncel kayıt şu anda yeniden okunamadı; formdaki değerleriniz korunuyor.";
      return {
        status: partiallySaved ? "partial" : "error",
        message: `${completed}${context.failedOperation} başka bir işlem tarafından değiştirildiği için tamamlanamadı.${refreshMessage}`,
        traceId: error.problem.traceId,
        productId: context.productId,
        reloadHref: context.productId && context.currentRecordAvailable
          ? `/products/${encodeURIComponent(context.productId)}?reload=1`
          : undefined,
      };
    }

    if (error.problem.status === 409) {
      const conflictDetail = context.conflictMessage || error.problem.detail || "Bu değer başka bir kayıtta kullanılıyor.";
      const completed = partiallySaved ? `${summarizeOperations(context.completedOperations)} kaydedildi; ancak ` : "";
      const fieldErrors = error.problem.code === "product_variant_sku_conflict"
        ? error.problem.errors
        : context.conflictField
          ? { [context.conflictField]: [conflictDetail] }
          : error.problem.errors;
      return {
        status: partiallySaved ? "partial" : "error",
        message: `${completed}${context.failedOperation} tamamlanamadı: ${conflictDetail}`,
        traceId: error.problem.traceId,
        productId: context.productId,
        fieldErrors,
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

// Burada API'nin variants[n].field anahtarlarını formun gerçek görünür satır indekslerine taşırım.
export function remapProductVariantBulkError(error: unknown, formIndexes: number[]): unknown {
  if (!(error instanceof ApiError) || !error.problem.errors) return error;

  const errors = Object.entries(error.problem.errors).reduce<Record<string, string[]>>(
    (mapped, [field, messages]) => {
      const match = /^variants\[(\d+)]\.(.+)$/.exec(field);
      if (!match) {
        mapped[field] = messages;
        return mapped;
      }

      const bulkIndex = Number(match[1]);
      const formIndex = formIndexes[bulkIndex];
      mapped[formIndex === undefined ? field : `variants.${formIndex}.${match[2]}`] = messages;
      return mapped;
    },
    {},
  );

  return new ApiError({ ...error.problem, errors });
}

// Burada uzun varyant listelerinde hata mesajını okunabilir bir tamamlanan işlem özetiyle sınırlandırıyorum.
function summarizeOperations(operations: string[]): string {
  if (operations.length <= 3) return operations.join(", ");
  return `${operations.slice(0, 2).join(", ")} ve ${operations.length - 2} işlem daha`;
}
