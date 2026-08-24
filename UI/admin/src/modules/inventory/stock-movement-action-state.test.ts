import { describe, expect, it, vi } from "vitest";
import { runStockMovementAction } from "./stock-movement-action-state";

describe("runStockMovementAction", () => {
  it("returns success and closes pending before route reconciliation", async () => {
    const pendingStates: boolean[] = [];
    const action = vi.fn().mockResolvedValue({
      status: "success",
      message: "1 stok hareketi atomik olarak kaydedildi.",
      movementCount: 1,
    });

    const result = await runStockMovementAction(
      action,
      new FormData(),
      (pending) => pendingStates.push(pending),
    );

    expect(result).toMatchObject({ status: "success", movementCount: 1 });
    expect(pendingStates).toEqual([true, false]);
  });

  it("closes pending and returns a safe error when the action rejects", async () => {
    const pendingStates: boolean[] = [];

    const result = await runStockMovementAction(
      vi.fn().mockRejectedValue(new Error("transport failure")),
      new FormData(),
      (pending) => pendingStates.push(pending),
    );

    expect(result).toEqual({
      status: "error",
      message: "Stok hareketleri kaydedilemedi. Bağlantıyı kontrol edip tekrar deneyin.",
    });
    expect(pendingStates).toEqual([true, false]);
  });
});
