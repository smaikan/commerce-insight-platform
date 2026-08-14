"use client";

import { createContext, useContext, useEffect, useState } from "react";

export type HeaderSessionState = "loading" | "guest" | "authenticated";

type ResolvedHeaderSessionState = Exclude<HeaderSessionState, "loading">;

const HeaderSessionContext = createContext<HeaderSessionState>("guest");

let pendingHeaderSessionRequest: Promise<ResolvedHeaderSessionState> | null = null;

// Burada React geliştirme modundaki çift effect çalıştırmasında aynı oturum isteğini paylaşarak gereksiz istek ve AbortError üretmiyorum.
export function loadHeaderSessionState(): Promise<ResolvedHeaderSessionState> {
  if (!pendingHeaderSessionRequest) {
    pendingHeaderSessionRequest = fetch("/api/auth/session", {
      credentials: "same-origin",
      cache: "no-store",
    })
      .then(async (response) => {
        if (!response.ok) return "guest";

        const result = await response.json() as { authenticated?: unknown };
        return result.authenticated === true ? "authenticated" : "guest";
      })
      .catch(() => "guest" as const)
      .finally(() => {
        pendingHeaderSessionRequest = null;
      });
  }

  return pendingHeaderSessionRequest;
}

// Burada public sayfaları cookie nedeniyle dinamikleştirmeden navbar ve favoriler için tek oturum durumu isteği paylaştırıyorum.
export function HeaderSessionProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<HeaderSessionState>("loading");

  useEffect(() => {
    let isMounted = true;

    void loadHeaderSessionState().then((nextState) => {
      // Burada istek tamamlanmadan provider kaldırılırsa eski bileşenin state'ini güncellemiyorum.
      if (isMounted) {
        setState(nextState);
      }
    });

    return () => {
      isMounted = false;
    };
  }, []);

  return <HeaderSessionContext value={state}>{children}</HeaderSessionContext>;
}

// Burada navbar ve ürün favori kontrollerinin aynı oturum snapshot'ını okumasına izin veriyorum.
export function useHeaderSession(): HeaderSessionState {
  return useContext(HeaderSessionContext);
}
