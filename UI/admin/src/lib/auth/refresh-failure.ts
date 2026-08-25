import { ApiError, retryAfterSeconds } from "../api/problem";

export type RefreshFailureDecision = {
  reason: "forbidden" | "refresh_rate_limited" | "session_expired" | "verification_failed";
  clearCookies: boolean;
  retryAfter?: number;
};

// Burada refresh hatasını yalnız kesin oturum geçersizliğinde cookie silecek güvenli bir route kararına dönüştürüyorum.
export function refreshFailureDecision(error: unknown, now = Date.now()): RefreshFailureDecision {
  if (!(error instanceof ApiError)) {
    return { reason: "session_expired", clearCookies: true };
  }

  if (error.problem.code === "invalid_auth_response") {
    return { reason: "session_expired", clearCookies: true };
  }

  if (error.problem.status === 403) {
    return { reason: "forbidden", clearCookies: true };
  }

  if (error.problem.status === 429) {
    return {
      reason: "refresh_rate_limited",
      clearCookies: false,
      retryAfter: retryAfterSeconds(error.problem.retryAfter, now),
    };
  }

  if (error.problem.status >= 500) {
    return { reason: "verification_failed", clearCookies: false };
  }

  return { reason: "session_expired", clearCookies: true };
}
