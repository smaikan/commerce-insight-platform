import { describe, expect, it } from "vitest";

import { resolveCheckoutEnterAction } from "./enter-action";

describe("checkout Enter davranışı", () => {
  // Burada kupon alanındaki Enter'ın checkout yerine kupon uygulama niyeti ürettiğini doğruluyorum.
  it("applies the coupon from the coupon input", () => {
    expect(resolveCheckoutEnterAction({
      key: "Enter",
      isComposing: false,
      tagName: "input",
      name: "couponCode",
      inputType: "text",
    })).toBe("apply-coupon");
  });

  // Burada diğer checkout alanlarının Enter ile implicit form submit oluşturmadığını doğruluyorum.
  it.each([
    { tagName: "input", name: "customerEmail", inputType: "email" },
    { tagName: "input", name: "shippingFirstName", inputType: "text" },
    { tagName: "input", name: "shippingMethodId", inputType: "radio" },
    { tagName: "select", name: "shippingCity" },
  ])("prevents checkout from $name", (target) => {
    expect(resolveCheckoutEnterAction({ key: "Enter", isComposing: false, ...target }))
      .toBe("prevent-checkout");
  });

  // Burada textarea, bağlantı ve açıkça odaklanmış butonların erişilebilir doğal Enter davranışını koruyorum.
  it.each([
    { tagName: "textarea", name: "shippingFullAddress" },
    { tagName: "a" },
    { tagName: "button" },
    { tagName: "input", inputType: "submit" },
  ])("allows the native Enter behavior for $tagName", (target) => {
    expect(resolveCheckoutEnterAction({ key: "Enter", isComposing: false, ...target }))
      .toBe("allow");
  });

  // Burada IME ile metin oluşturma ve Enter dışındaki tuşların form davranışına müdahale etmediğini doğruluyorum.
  it("allows composition and non-Enter keyboard input", () => {
    expect(resolveCheckoutEnterAction({
      key: "Enter",
      isComposing: true,
      tagName: "input",
      name: "customerEmail",
    })).toBe("allow");
    expect(resolveCheckoutEnterAction({
      key: "Tab",
      isComposing: false,
      tagName: "input",
      name: "customerEmail",
    })).toBe("allow");
  });
});
