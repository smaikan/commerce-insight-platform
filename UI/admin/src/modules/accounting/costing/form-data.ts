import type { OpeningCostActionState, OpeningCostDraft } from "./types";

const MONEY_PATTERN = /^\d{1,16}(?:[.,]\d{1,2})?$/;

// Burada para alanlarını API ve decimal(18,2) saklama hassasiyetiyle aynı sınırda doğruluyorum.
export function parseOpeningCostForm(formData: FormData):
  | { ok: true; draft: OpeningCostDraft; input: { expectedConcurrencyToken: string; unitCostExcludingVat: number; unitCostIncludingVat?: number } }
  | { ok: false; state: OpeningCostActionState } {
  const draft: OpeningCostDraft = {
    layerId: value(formData, "layerId"),
    productVariantId: value(formData, "productVariantId"),
    expectedConcurrencyToken: value(formData, "expectedConcurrencyToken"),
    unitCostExcludingVat: value(formData, "unitCostExcludingVat"),
    unitCostIncludingVat: value(formData, "unitCostIncludingVat"),
  };
  const fieldErrors: Record<string, string[]> = {};
  if (!isUuid(draft.layerId)) fieldErrors.layerId = ["Maliyet katmanı geçersiz."];
  if (!isUuid(draft.productVariantId)) fieldErrors.productVariantId = ["Ürün varyantı geçersiz."];
  if (!isUuid(draft.expectedConcurrencyToken)) fieldErrors.expectedConcurrencyToken = ["Kayıt sürümü geçersiz; sayfayı yenileyin."];
  if (!MONEY_PATTERN.test(draft.unitCostExcludingVat)) fieldErrors.unitCostExcludingVat = ["KDV hariç birim maliyet 0 veya daha büyük ve en fazla iki ondalıklı olmalıdır."];
  if (draft.unitCostIncludingVat && !MONEY_PATTERN.test(draft.unitCostIncludingVat)) fieldErrors.unitCostIncludingVat = ["KDV dahil birim maliyet en fazla iki ondalıklı olmalıdır."];
  if (Object.keys(fieldErrors).length) return { ok: false, state: { status: "error", message: "Maliyet alanlarını kontrol edin.", draft, fieldErrors } };
  return {
    ok: true,
    draft,
    input: {
      expectedConcurrencyToken: draft.expectedConcurrencyToken,
      unitCostExcludingVat: money(draft.unitCostExcludingVat),
      ...(draft.unitCostIncludingVat ? { unitCostIncludingVat: money(draft.unitCostIncludingVat) } : {}),
    },
  };
}

function value(formData: FormData, key: string): string {
  const candidate = formData.get(key);
  return typeof candidate === "string" ? candidate.trim() : "";
}

function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function money(value: string): number {
  return Number(value.replace(",", "."));
}
