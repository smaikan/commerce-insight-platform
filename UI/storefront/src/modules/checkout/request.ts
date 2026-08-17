import { isUuid } from "../../lib/validation/identifiers";
import type {
  GuestAddressRequest,
  GuestCheckoutRequest,
  MemberCheckoutRequest,
} from "@/modules/checkout/types";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const IDEMPOTENCY_KEY_PATTERN = /^[A-Za-z0-9_-]{16,80}$/;

function requiredString(value: unknown, maximumLength: number): string | null {
  if (typeof value !== "string") return null;
  const normalized = value.trim();
  return normalized && normalized.length <= maximumLength ? normalized : null;
}

function optionalString(value: unknown, maximumLength: number): string | undefined | null {
  if (value === undefined || value === null || value === "") return undefined;
  if (typeof value !== "string") return null;
  const normalized = value.trim();
  return normalized.length <= maximumLength ? normalized || undefined : null;
}

// Burada adres girdisini yalnızca guest order snapshot sözleşmesinin izin verdiği alanlara ve uzunluklara indiriyorum.
function parseAddress(value: unknown): GuestAddressRequest | null {
  if (!value || typeof value !== "object") return null;
  const source = value as Record<string, unknown>;
  const title = requiredString(source.title, 100);
  const firstName = requiredString(source.firstName, 100);
  const lastName = requiredString(source.lastName, 100);
  const phoneNumber = requiredString(source.phoneNumber, 30);
  const city = requiredString(source.city, 100);
  const district = requiredString(source.district, 100);
  const neighborhood = optionalString(source.neighborhood, 100);
  const fullAddress = requiredString(source.fullAddress, 500);
  const postalCode = optionalString(source.postalCode, 20);

  if (!title || !firstName || !lastName || !phoneNumber || !city || !district || neighborhood === null || !fullAddress || postalCode === null) {
    return null;
  }

  return {
    title,
    firstName,
    lastName,
    phoneNumber,
    city,
    district,
    ...(neighborhood ? { neighborhood } : {}),
    fullAddress,
    ...(postalCode ? { postalCode } : {}),
  };
}

// Burada checkout gövdesinden fiyat, vergi, stok ve toplam gibi browser'ın belirleyemeyeceği alanları tamamen dışarıda bırakıyorum.
export function parseGuestCheckoutRequest(value: unknown): GuestCheckoutRequest | null {
  if (!value || typeof value !== "object") return null;
  const source = value as Record<string, unknown>;
  const customerSource = source.customer;
  if (!customerSource || typeof customerSource !== "object") return null;

  const customer = customerSource as Record<string, unknown>;
  const firstName = requiredString(customer.firstName, 100);
  const lastName = requiredString(customer.lastName, 100);
  const email = requiredString(customer.email, 320);
  const phoneNumber = requiredString(customer.phoneNumber, 30);
  const shippingAddress = parseAddress(source.shippingAddress);
  const billingAddress = source.billingAddress === undefined || source.billingAddress === null
    ? undefined
    : parseAddress(source.billingAddress);
  const couponCode = optionalString(source.couponCode, 50);

  if (
    !isUuid(source.expectedCartConcurrencyToken) ||
    !isUuid(source.shippingMethodId) ||
    !firstName ||
    !lastName ||
    !email ||
    !EMAIL_PATTERN.test(email) ||
    !phoneNumber ||
    !shippingAddress ||
    billingAddress === null ||
    couponCode === null
  ) {
    return null;
  }

  return {
    expectedCartConcurrencyToken: source.expectedCartConcurrencyToken,
    customer: { firstName, lastName, email, phoneNumber },
    shippingAddress,
    ...(billingAddress ? { billingAddress } : {}),
    shippingMethodId: source.shippingMethodId,
    ...(couponCode ? { couponCode } : {}),
  };
}

// Burada üye checkout gövdesini yalnız cart tokenı, sahiplik denetimli adres, kargo ve opsiyonel kupon alanlarına indiriyorum.
export function parseMemberCheckoutRequest(value: unknown): MemberCheckoutRequest | null {
  if (!value || typeof value !== "object") return null;
  const source = value as Record<string, unknown>;
  const couponCode = optionalString(source.couponCode, 50);
  if (
    !isUuid(source.expectedCartConcurrencyToken)
    || !isUuid(source.shippingAddressId)
    || !isUuid(source.shippingMethodId)
    || couponCode === null
  ) return null;

  return {
    expectedCartConcurrencyToken: source.expectedCartConcurrencyToken,
    shippingAddressId: source.shippingAddressId,
    shippingMethodId: source.shippingMethodId,
    ...(couponCode ? { couponCode } : {}),
  };
}

export function parseIdempotencyKey(value: string | null): string | null {
  const normalized = value?.trim() || "";
  return IDEMPOTENCY_KEY_PATTERN.test(normalized) ? normalized : null;
}

export function parseTurnstileToken(value: string | null): string | undefined | null {
  if (!value) return undefined;
  const normalized = value.trim();
  return normalized && normalized.length <= 4_096 && !/[\r\n]/.test(normalized) ? normalized : null;
}
