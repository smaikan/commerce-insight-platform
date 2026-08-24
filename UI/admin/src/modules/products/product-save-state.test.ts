import { describe, expect, it } from "vitest";
import { isProductActionAwaitingResult } from "./product-save-state";
import type { ProductActionState } from "./types";

describe("product save state", () => {
  it("keeps pending while the submitted action has not returned a new state", () => {
    const submittedState: ProductActionState = { status: "idle" };

    expect(isProductActionAwaitingResult(true, submittedState, submittedState)).toBe(true);
  });

  it("stops showing save pending after the server action returns even if navigation is still pending", () => {
    const submittedState: ProductActionState = { status: "idle" };
    const returnedState: ProductActionState = {
      status: "success",
      productId: "P00042",
      completionToken: "completed",
    };

    expect(isProductActionAwaitingResult(true, returnedState, submittedState)).toBe(false);
  });
});
