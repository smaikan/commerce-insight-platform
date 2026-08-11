import { describe, expect, it } from "vitest";

import {
  checkoutProblemMessage,
  isCheckoutChallengeRequired,
} from "./checkout-api";

describe("checkout challenge recovery", () => {
  // Burada yalnız doğru status ve problem code birleşiminin güvenlik doğrulama akışını açtığını doğruluyorum.
  it("recognizes the documented Turnstile challenge", () => {
    expect(isCheckoutChallengeRequired({ status: 428, code: "guest_checkout_challenge_required" })).toBe(true);
    expect(isCheckoutChallengeRequired({ status: 428, code: "another_problem" })).toBe(false);
    expect(isCheckoutChallengeRequired({ status: 409, code: "guest_checkout_challenge_required" })).toBe(false);
  });

  // Burada kullanıcı mesajının ortam yapılandırması sızdırmadan gerçek kurtarma adımını anlattığını doğruluyorum.
  it("uses an actionable challenge message", () => {
    expect(checkoutProblemMessage({ status: 428, code: "guest_checkout_challenge_required" }))
      .toBe("Devam etmek için güvenlik doğrulamasını tamamlayın.");
  });
});
