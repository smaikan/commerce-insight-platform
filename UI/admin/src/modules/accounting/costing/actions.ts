"use server";

import { revalidatePath } from "next/cache";
import { ApiError } from "@/lib/api/problem";
import { requireAdminActionSession } from "@/lib/auth/session";
import { getOpeningBalanceLayer, updateOpeningBalanceLayer } from "./api";
import { parseOpeningCostForm } from "./form-data";
import type { OpeningCostActionState } from "./types";

// Burada stale tokenı otomatik tekrar kullanmadan güncel katmanı kullanıcı karşılaştırmasına geri taşıyorum.
export async function updateOpeningCostAction(_previous: OpeningCostActionState, formData: FormData): Promise<OpeningCostActionState> {
  const parsed = parseOpeningCostForm(formData);
  if (!parsed.ok) return parsed.state;
  try {
    const session = await requireAdminActionSession();
    await updateOpeningBalanceLayer(parsed.draft.layerId, parsed.input, session);
    revalidatePath("/accounting");
    revalidatePath("/accounting/costing");
    revalidatePath("/accounting/reports");
    return { status: "success", message: "Açılış stok maliyeti güncellendi.", refresh: true };
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 409) {
      try {
        const session = await requireAdminActionSession();
        const currentLayer = await getOpeningBalanceLayer(parsed.draft.productVariantId, session);
        if (currentLayer) return { status: "conflict", message: "Kayıt siz formu doldururken değişti. Taslağınız korundu; güncel değerleri karşılaştırıp yeniden onaylayın.", draft: parsed.draft, currentLayer, traceId: error.problem.traceId };
        return { status: "error", message: "Çakışan açılış katmanı artık bulunamıyor. Taslağınız korundu.", draft: parsed.draft, traceId: error.problem.traceId };
      } catch (refreshError) {
        const state = costError(refreshError, parsed.draft);
        return { ...state, message: "Kayıt değişti ancak güncel maliyet şu anda alınamadı. Taslağınız korundu; sayfayı yenileyip tekrar deneyin." };
      }
    }
    return costError(error, parsed.draft);
  }
}

function costError(error: unknown, draft: OpeningCostActionState["draft"]): OpeningCostActionState {
  if (!(error instanceof ApiError)) return { status: "error", message: "Maliyet güncellenemedi.", draft };
  const problem = error.problem;
  const message = problem.status === 401 ? "Oturumunuz sona erdi. Yeniden giriş yapın." : problem.status === 403 ? "Bu işlem için yönetici yetkiniz yok." : problem.status === 404 ? "Açılış maliyet katmanı artık bulunamıyor." : problem.status === 429 ? "İstek sınırına ulaşıldı; daha sonra aynı taslakla tekrar deneyin." : problem.detail || problem.title || "Maliyet güncellenemedi.";
  return { status: "error", message, draft, fieldErrors: problem.errors, traceId: problem.traceId };
}
