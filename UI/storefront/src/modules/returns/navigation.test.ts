import { describe, expect, it } from "vitest";

import { guestOrderConfirmationHref } from "./navigation";

describe("guest order navigation", () => {
  // Burada magic-link exchange sonrasının iade formu yerine sipariş confirmation ekranına gittiğini doğruluyorum.
  it("routes verified access to the guest confirmation page", () => {
    expect(guestOrderConfirmationHref("order-id")).toBe("/checkout/confirmation/order-id?access=guest");
  });
});
