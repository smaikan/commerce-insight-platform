import { describe, expect, it } from "vitest";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { runOrderStatusAction } from "./status-action-state";

describe("order status action state", () => {
  it("clears mutation pending immediately after a successful action result", async () => {
    const pendingStates: boolean[] = [];
    const result = await runOrderStatusAction(async () => ({
      status: "success",
      message: "Sipariş durumu güncellendi.",
    }), new FormData(), (pending) => pendingStates.push(pending));

    expect(result.status).toBe("success");
    expect(pendingStates).toEqual([true, false]);
  });

  it("clears mutation pending and returns a safe retry result after transport rejection", async () => {
    const pendingStates: boolean[] = [];
    const result = await runOrderStatusAction(async (): Promise<AdminMutationResult> => {
      throw new Error("transport failed");
    }, new FormData(), (pending) => pendingStates.push(pending));

    expect(result).toEqual({
      status: "error",
      message: "Sipariş durumu güncellenemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    });
    expect(pendingStates).toEqual([true, false]);
  });
});
