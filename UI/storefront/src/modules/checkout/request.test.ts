import { describe, expect, it } from "vitest";

import {
  parseGuestCheckoutRequest,
  parseIdempotencyKey,
  parseMemberCheckoutRequest,
  parseTurnstileToken,
} from "./request";

const cartToken = "77a50a8f-8dc1-4aa7-aed0-71ac4e58a8bb";
const shippingMethodId = "893fdb48-e9cf-4c8c-a94c-3b989697f204";

function validRequest() {
  return {
    expectedCartConcurrencyToken: cartToken,
    customer: { firstName: "Ada", lastName: "Lovelace", email: "ada@example.com", phoneNumber: "+905551112233" },
    shippingAddress: {
      title: "Ev",
      firstName: "Ada",
      lastName: "Lovelace",
      phoneNumber: "+905551112233",
      city: "İstanbul",
      district: "Kadıköy",
      fullAddress: "Örnek Sokak 1",
      postalCode: "34000",
    },
    billingAddress: null,
    shippingMethodId,
    couponCode: "WELCOME10",
    grandTotal: 1,
    userId: "U00001",
  };
}

describe("guest checkout request", () => {
  // Burada fiyat, toplam ve kullanıcı kimliği gibi browser'ın belirleyemeyeceği alanların allowlist dışında kaldığını doğruluyorum.
  it("keeps only documented checkout fields", () => {
    expect(parseGuestCheckoutRequest(validRequest())).toEqual({
      expectedCartConcurrencyToken: cartToken,
      customer: { firstName: "Ada", lastName: "Lovelace", email: "ada@example.com", phoneNumber: "+905551112233" },
      shippingAddress: {
        title: "Ev",
        firstName: "Ada",
        lastName: "Lovelace",
        phoneNumber: "+905551112233",
        city: "İstanbul",
        district: "Kadıköy",
        fullAddress: "Örnek Sokak 1",
        postalCode: "34000",
      },
      shippingMethodId,
      couponCode: "WELCOME10",
    });
  });

  // Burada zorunlu müşteri, adres, e-posta ve UUID alanları bozuksa isteğin upstream'e ulaşmadığını doğruluyorum.
  it("rejects malformed checkout fields", () => {
    expect(parseGuestCheckoutRequest({ ...validRequest(), shippingMethodId: "bad" })).toBeNull();
    expect(parseGuestCheckoutRequest({ ...validRequest(), customer: { firstName: "Ada", lastName: "Lovelace", email: "bad", phoneNumber: "1" } })).toBeNull();
    expect(parseGuestCheckoutRequest({ ...validRequest(), shippingAddress: null })).toBeNull();
  });

  // Burada idempotency ve opsiyonel challenge header değerlerinin dar karakter/uzunluk sınırında kaldığını doğruluyorum.
  it("validates checkout headers", () => {
    expect(parseIdempotencyKey("12345678-1234-1234-1234-123456789012")).toBe("12345678-1234-1234-1234-123456789012");
    expect(parseIdempotencyKey("short")).toBeNull();
    expect(parseIdempotencyKey("123456789012345.invalid")).toBeNull();
    expect(parseIdempotencyKey("a".repeat(81))).toBeNull();
    expect(parseTurnstileToken(null)).toBeUndefined();
    expect(parseTurnstileToken("token\nvalue")).toBeNull();
  });

  // Burada üye sipariş gövdesinde yalnız cart tokenı, sahiplik denetimli adres, kargo ve kupon alanlarının kaldığını doğruluyorum.
  it("keeps only documented member checkout fields", () => {
    expect(parseMemberCheckoutRequest({
      expectedCartConcurrencyToken: cartToken,
      shippingAddressId: "2fe43ff2-d6d7-43a4-91f0-cf8a1974aaba",
      shippingMethodId,
      couponCode: "WELCOME10",
      grandTotal: 1,
      userId: "U00001",
    })).toEqual({
      expectedCartConcurrencyToken: cartToken,
      shippingAddressId: "2fe43ff2-d6d7-43a4-91f0-cf8a1974aaba",
      shippingMethodId,
      couponCode: "WELCOME10",
    });
    expect(parseMemberCheckoutRequest({ expectedCartConcurrencyToken: cartToken, shippingAddressId: "bad", shippingMethodId })).toBeNull();
  });
});
