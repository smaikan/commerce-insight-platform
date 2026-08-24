import { supportsManualStockMovement } from "./stock-movement-rules";
import type { BulkStockMovement, StockMovementActionState } from "./types";

const maximumVariantSkuLength = 100;

type ParseBulkMovementsResult =
  | { ok: true; movements: BulkStockMovement[] }
  | { ok: false; state: StockMovementActionState };

// Burada form taslağını SKU tabanlı atomik stok hareketi sözleşmesine dönüştürüyorum.
export function parseBulkMovements(raw: FormDataEntryValue | null): ParseBulkMovementsResult {
  if (typeof raw !== "string") {
    return { ok: false, state: { status: "error", message: "Hareket satırları bulunamadı." } };
  }

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
    if (!isRecord(item)) return rowError(index, "row", "Satır biçimi geçerli değil.");

    const productVariantSku = typeof item.productVariantSku === "string" ? item.productVariantSku.trim() : "";
    const type = Number(item.type);
    const direction = Number(item.direction);
    const quantity = Number(item.quantity);
    const reason = typeof item.reason === "string" ? item.reason.trim() : "";

    if (!productVariantSku) return rowError(index, "productVariantSku", "Varyant SKU zorunludur.");
    if (productVariantSku.length > maximumVariantSkuLength) {
      return rowError(index, "productVariantSku", `Varyant SKU en fazla ${maximumVariantSkuLength} karakter olabilir.`);
    }
    if (!Number.isInteger(quantity) || quantity <= 0 || quantity > 2_147_483_647) {
      return rowError(index, "quantityDelta", "Miktar pozitif tam sayı olmalı.");
    }
    if (!supportsManualStockMovement(type, direction)) {
      return rowError(index, "type", "Hareket türü ve yön birbiriyle uyumlu değil.");
    }
    if (reason.length > 500) return rowError(index, "reason", "Açıklama en fazla 500 karakter olabilir.");

    movements.push({
      productVariantSku,
      type,
      quantityDelta: direction === 1 ? quantity : -quantity,
      reason: reason || null,
    });
  }

  return { ok: true, movements };
}

// Burada API ve yerel doğrulama alan adlarından ilgili satırın ilk hata mesajını buluyorum.
export function getMovementFieldError(
  fieldErrors: Record<string, string[]> | undefined,
  index: number,
  field: "productVariantSku" | "quantityDelta" | "type" | "reason",
): string | undefined {
  if (!fieldErrors) return undefined;

  const pascalField = `${field[0].toUpperCase()}${field.slice(1)}`;
  const candidates = [
    `movements[${index}].${field}`,
    `Movements[${index}].${pascalField}`,
  ];

  for (const key of candidates) {
    const message = fieldErrors[key]?.[0];
    if (message) return message;
  }

  return undefined;
}

// Burada satır bazlı doğrulama hatasını hem özet hem alan mesajıyla hazırlıyorum.
function rowError(index: number, field: string, message: string): ParseBulkMovementsResult {
  return {
    ok: false,
    state: {
      status: "error",
      message: `${index + 1}. satır: ${message}`,
      fieldErrors: { [`movements[${index}].${field}`]: [message] },
    },
  };
}

// Burada JSON içeriğinin nesne olup olmadığını güvenli biçimde ayırıyorum.
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
