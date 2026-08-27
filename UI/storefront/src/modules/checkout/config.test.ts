import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { getSandboxPaymentInfo, isCheckoutOrderCreationEnabled } from "./config";

const originalValue = process.env.CHECKOUT_ORDER_CREATION_ENABLED;
const originalPaymentEnvironment = process.env.CHECKOUT_PAYMENT_ENVIRONMENT;
const originalNodeEnv = process.env.NODE_ENV;
const mutableEnv = process.env as Record<string, string | undefined>;

afterEach(() => {
  if (originalValue === undefined) delete process.env.CHECKOUT_ORDER_CREATION_ENABLED;
  else process.env.CHECKOUT_ORDER_CREATION_ENABLED = originalValue;
  if (originalPaymentEnvironment === undefined) delete process.env.CHECKOUT_PAYMENT_ENVIRONMENT;
  else process.env.CHECKOUT_PAYMENT_ENVIRONMENT = originalPaymentEnvironment;
  if (originalNodeEnv === undefined) delete mutableEnv.NODE_ENV;
  else mutableEnv.NODE_ENV = originalNodeEnv;
});

describe("sandbox payment information", () => {
  // Burada eksik, hatalı veya production yapılandırmasında test kartının istemciye hiç geçirilmediğini doğruluyorum.
  it("fails closed outside an explicit sandbox environment", () => {
    delete process.env.CHECKOUT_PAYMENT_ENVIRONMENT;
    expect(getSandboxPaymentInfo()).toBeNull();

    process.env.CHECKOUT_PAYMENT_ENVIRONMENT = "production";
    expect(getSandboxPaymentInfo()).toBeNull();

    process.env.CHECKOUT_PAYMENT_ENVIRONMENT = "test";
    expect(getSandboxPaymentInfo()).toBeNull();
  });

  // Burada yalnız sandbox değerinde onaylanan iyzico test kartının görünür olduğunu doğruluyorum.
  it("returns the approved card only for sandbox", () => {
    process.env.CHECKOUT_PAYMENT_ENVIRONMENT = " SANDBOX ";

    expect(getSandboxPaymentInfo()).toEqual({ cardNumber: "4543590000000006" });
  });
});

describe("checkout order creation flag", () => {
  // Burada eksik veya kapalı yapılandırmanın canlı sipariş oluşturmayı güvenli biçimde kapattığını doğruluyorum.
  it("fails closed unless explicitly enabled", () => {
    delete process.env.CHECKOUT_ORDER_CREATION_ENABLED;
    expect(isCheckoutOrderCreationEnabled()).toBe(false);

    process.env.CHECKOUT_ORDER_CREATION_ENABLED = "false";
    expect(isCheckoutOrderCreationEnabled()).toBe(false);
  });

  // Burada yalnız açıkça verilen true değerinin yerel geliştirme sipariş akışını etkinleştirdiğini doğruluyorum.
  it("allows an explicit enabled environment", () => {
    mutableEnv.NODE_ENV = "development";
    process.env.CHECKOUT_ORDER_CREATION_ENABLED = " TRUE ";
    expect(isCheckoutOrderCreationEnabled()).toBe(true);
  });

  // Burada ödeme entegrasyonu sonrasında production ortamının da yalnız açık feature flag ile etkinleştiğini doğruluyorum.
  it("allows production only when the feature flag is explicitly enabled", () => {
    mutableEnv.NODE_ENV = "production";
    process.env.CHECKOUT_ORDER_CREATION_ENABLED = "true";
    expect(isCheckoutOrderCreationEnabled()).toBe(true);
  });
});
