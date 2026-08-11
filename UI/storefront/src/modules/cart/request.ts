import type {
  AddCartItemRequest,
  CartConcurrencyRequest,
  UpdateCartItemRequest,
} from "@/modules/cart/types";
import { isUuid } from "../../lib/validation/identifiers";

export { isUuid } from "../../lib/validation/identifiers";

// Burada browser gövdesini doğrulayıp yalnız backend'in izin verdiği varyant, adet ve concurrency alanlarına indiriyorum.
export function parseAddCartItemRequest(value: unknown): AddCartItemRequest | null {
  if (!value || typeof value !== "object") return null;

  const source = value as Record<string, unknown>;
  const token = source.expectedConcurrencyToken;
  if (
    !isUuid(source.productVariantId) ||
    typeof source.quantity !== "number" ||
    !Number.isSafeInteger(source.quantity) ||
    source.quantity <= 0 ||
    !(token === undefined || token === null || isUuid(token))
  ) {
    return null;
  }

  return {
    productVariantId: source.productVariantId,
    quantity: source.quantity,
    ...(typeof token === "string" ? { expectedConcurrencyToken: token } : {}),
  };
}

// Burada adet güncellemesini yalnızca API'nin kabul ettiği pozitif adet ve güncel concurrency token alanlarına indiriyorum.
export function parseUpdateCartItemRequest(value: unknown): UpdateCartItemRequest | null {
  if (!value || typeof value !== "object") return null;

  const source = value as Record<string, unknown>;
  if (
    typeof source.quantity !== "number" ||
    !Number.isSafeInteger(source.quantity) ||
    source.quantity <= 0 ||
    !isUuid(source.expectedConcurrencyToken)
  ) {
    return null;
  }

  return {
    quantity: source.quantity,
    expectedConcurrencyToken: source.expectedConcurrencyToken,
  };
}

// Burada silme işlemlerinde backend'in zorunlu tuttuğu son concurrency token dışında hiçbir browser alanını taşımıyorum.
export function parseCartConcurrencyRequest(value: unknown): CartConcurrencyRequest | null {
  if (!value || typeof value !== "object") return null;

  const source = value as Record<string, unknown>;
  return isUuid(source.expectedConcurrencyToken)
    ? { expectedConcurrencyToken: source.expectedConcurrencyToken }
    : null;
}
