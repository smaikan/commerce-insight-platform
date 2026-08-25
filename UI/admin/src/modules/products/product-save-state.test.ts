import { describe, expect, it, vi } from "vitest";
import { isProductActionAwaitingResult, navigateAfterSuccessfulProductSave } from "./product-save-state";
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

  it("treats a completed action token as authoritative even if state references match", () => {
    const completedState: ProductActionState = {
      status: "success",
      productId: "P00042",
      completionToken: "completed",
    };

    expect(isProductActionAwaitingResult(true, completedState, completedState)).toBe(false);
  });

  it("navigates once to the revalidated product page after a successful save", () => {
    const navigator = {
      replace: vi.fn(),
      refresh: vi.fn(),
    };

    navigateAfterSuccessfulProductSave(navigator, "P00042", "edit");

    expect(navigator.replace).toHaveBeenCalledOnce();
    expect(navigator.replace).toHaveBeenCalledWith("/products/P00042?saved=1");
    expect(navigator.refresh).not.toHaveBeenCalled();
  });
});
