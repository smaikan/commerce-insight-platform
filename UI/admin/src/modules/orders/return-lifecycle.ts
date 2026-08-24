import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import type { ReturnRequest } from "@/modules/orders/types";

export type ReturnActionIntent = "approve" | "reject" | "receive" | "complete";

type ReturnLifecycleSnapshot = Pick<
  ReturnRequest,
  "status" | "approvedAt" | "rejectedAt" | "receivedAt" | "completedAt"
>;

// Burada yeni teslim-önce karar akışını ve yalnız timestamp ile doğrulanan legacy geçişleri tek allowlist'te tutuyorum.
export function availableReturnActions(returnRequest: ReturnLifecycleSnapshot): ReturnActionIntent[] {
  const { status, approvedAt, rejectedAt, receivedAt, completedAt } = returnRequest;

  if (status === 0 && !approvedAt && !rejectedAt && !receivedAt && !completedAt) {
    return ["receive"];
  }

  if (status === 3 && receivedAt && !approvedAt && !rejectedAt && !completedAt) {
    return ["reject", "approve"];
  }

  if (status === 1 && approvedAt && !receivedAt && !rejectedAt && !completedAt) {
    return ["receive"];
  }

  if (status === 3 && approvedAt && receivedAt && !rejectedAt && !completedAt) {
    return ["complete"];
  }

  return [];
}

// Burada route yenilemesi sürse bile Server Action yeni sonuç döndürdüğünde iade kontrolünün beklemesini kapatıyorum.
export function isReturnActionAwaitingResult(
  actionPending: boolean,
  currentState: AdminMutationResult | null,
  stateAtSubmit: AdminMutationResult | null,
): boolean {
  return actionPending && currentState === stateAtSubmit;
}
