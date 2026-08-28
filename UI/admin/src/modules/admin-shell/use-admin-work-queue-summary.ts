"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { isAdminWorkQueueSummary } from "@/modules/admin-shell/work-queue";
import type { AdminWorkQueueSummaryData } from "@/modules/dashboard/types";

const WORK_QUEUE_REFRESH_INTERVAL_MS = 15_000;
const DESKTOP_MEDIA_QUERY = "(min-width: 1024px)";

export type AdminNavigationMode = "desktop" | "mobile";

type WorkQueueState = {
  summary: AdminWorkQueueSummaryData | null;
  unavailable: boolean;
};

// Burada yalnız görünür navigasyon örneğinin sayaçları periyodik ve odak dönüşlerinde yenilemesini sağlıyorum.
export function useAdminWorkQueueSummary(
  initialSummary: AdminWorkQueueSummaryData | null,
  initialUnavailable: boolean,
  mode: AdminNavigationMode,
): WorkQueueState {
  const [state, setState] = useState<WorkQueueState>({
    summary: initialSummary,
    unavailable: initialUnavailable,
  });
  const [enabled, setEnabled] = useState(false);
  const requestControllerRef = useRef<AbortController | null>(null);

  // Burada breakpoint değişiminde yalnız ekranda olan masaüstü veya mobil menüyü etkinleştiriyorum.
  useEffect(() => {
    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const updateEnabled = () => setEnabled(mode === "desktop" ? mediaQuery.matches : !mediaQuery.matches);
    updateEnabled();
    mediaQuery.addEventListener("change", updateEnabled);
    return () => mediaQuery.removeEventListener("change", updateEnabled);
  }, [mode]);

  // Burada son başarılı değerleri koruyarak BFF üzerinden güncel sayaçları okuyorum.
  const refresh = useCallback(async () => {
    requestControllerRef.current?.abort();
    const controller = new AbortController();
    requestControllerRef.current = controller;
    try {
      const response = await fetch("/api/admin/work-queue-summary", {
        method: "GET",
        headers: { Accept: "application/json" },
        cache: "no-store",
        credentials: "same-origin",
        signal: controller.signal,
      });
      if (!response.ok) throw new Error(`Work queue request failed with ${response.status}.`);
      const payload: unknown = await response.json();
      if (!isAdminWorkQueueSummary(payload)) throw new Error("Work queue response is invalid.");
      setState({ summary: payload, unavailable: false });
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") return;
      setState((current) => ({ ...current, unavailable: true }));
    }
  }, []);

  // Burada görünür menüyü açılışta, 15 saniyede bir ve pencere ya da sekme odağı geri geldiğinde yeniliyorum.
  useEffect(() => {
    if (!enabled) {
      requestControllerRef.current?.abort();
      return;
    }

    const refreshWhenVisible = () => {
      if (document.visibilityState === "visible") void refresh();
    };
    const initialRefreshId = shouldRefreshImmediately(initialSummary, initialUnavailable)
      ? window.setTimeout(refreshWhenVisible, 0)
      : undefined;
    const intervalId = window.setInterval(refreshWhenVisible, WORK_QUEUE_REFRESH_INTERVAL_MS);
    window.addEventListener("focus", refreshWhenVisible);
    document.addEventListener("visibilitychange", refreshWhenVisible);
    return () => {
      if (initialRefreshId !== undefined) window.clearTimeout(initialRefreshId);
      window.clearInterval(intervalId);
      window.removeEventListener("focus", refreshWhenVisible);
      document.removeEventListener("visibilitychange", refreshWhenVisible);
      requestControllerRef.current?.abort();
    };
  }, [enabled, initialSummary, initialUnavailable, refresh]);

  return state;
}

// Burada taze server render değerini yeniden istemeyip eksik veya süresi geçmiş ilk değeri hemen yeniliyorum.
function shouldRefreshImmediately(summary: AdminWorkQueueSummaryData | null, unavailable: boolean): boolean {
  if (unavailable || !summary) return true;
  const generatedAt = Date.parse(summary.generatedAtUtc);
  return !Number.isFinite(generatedAt) || Date.now() - generatedAt >= WORK_QUEUE_REFRESH_INTERVAL_MS;
}
