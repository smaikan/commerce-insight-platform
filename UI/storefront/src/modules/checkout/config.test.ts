import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { isCheckoutOrderCreationEnabled } from "./config";

const originalValue = process.env.CHECKOUT_ORDER_CREATION_ENABLED;

afterEach(() => {
  if (originalValue === undefined) delete process.env.CHECKOUT_ORDER_CREATION_ENABLED;
  else process.env.CHECKOUT_ORDER_CREATION_ENABLED = originalValue;
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
    process.env.CHECKOUT_ORDER_CREATION_ENABLED = " TRUE ";
    expect(isCheckoutOrderCreationEnabled()).toBe(true);
  });
});
