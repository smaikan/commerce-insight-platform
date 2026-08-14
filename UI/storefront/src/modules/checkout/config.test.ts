import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { isCheckoutOrderCreationEnabled } from "./config";

const originalValue = process.env.CHECKOUT_ORDER_CREATION_ENABLED;
const originalNodeEnv = process.env.NODE_ENV;
const mutableEnv = process.env as Record<string, string | undefined>;

afterEach(() => {
  if (originalValue === undefined) delete process.env.CHECKOUT_ORDER_CREATION_ENABLED;
  else process.env.CHECKOUT_ORDER_CREATION_ENABLED = originalValue;
  if (originalNodeEnv === undefined) delete mutableEnv.NODE_ENV;
  else mutableEnv.NODE_ENV = originalNodeEnv;
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

  // Burada canlı ortamda yanlışlıkla true verilse bile ödeme öncesi sipariş mutation'ının açılamadığını doğruluyorum.
  it("keeps production disabled until payment integration is implemented", () => {
    mutableEnv.NODE_ENV = "production";
    process.env.CHECKOUT_ORDER_CREATION_ENABLED = "true";
    expect(isCheckoutOrderCreationEnabled()).toBe(false);
  });
});
