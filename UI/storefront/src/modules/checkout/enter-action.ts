export type CheckoutEnterAction = "allow" | "prevent-checkout" | "apply-coupon";

type CheckoutEnterTarget = {
  key: string;
  isComposing: boolean;
  tagName: string;
  name?: string;
  inputType?: string;
};

// Burada Enter tuşunun implicit checkout, kupon uygulama veya doğal kontrol davranışından hangisini çalıştıracağını belirliyorum.
export function resolveCheckoutEnterAction(target: CheckoutEnterTarget): CheckoutEnterAction {
  if (target.key !== "Enter" || target.isComposing) return "allow";

  const tagName = target.tagName.toUpperCase();
  if (tagName === "INPUT") {
    const inputType = (target.inputType || "text").toLowerCase();
    if (["button", "submit", "reset", "image"].includes(inputType)) return "allow";
    if (target.name === "couponCode") return "apply-coupon";
    return "prevent-checkout";
  }

  if (tagName === "SELECT") return "prevent-checkout";
  return "allow";
}
