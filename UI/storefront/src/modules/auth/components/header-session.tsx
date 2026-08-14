"use client";

import { createContext, useContext, useEffect, useState } from "react";

export type HeaderSessionState = "loading" | "guest" | "authenticated";

const HeaderSessionContext = createContext<HeaderSessionState>("guest");

// Burada public sayfaları cookie nedeniyle dinamikleştirmeden navbar ve favoriler için tek oturum durumu isteği paylaştırıyorum.
export function HeaderSessionProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<HeaderSessionState>("loading");

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/auth/session", {
      credentials: "same-origin",
      cache: "no-store",
      signal: controller.signal,
    })
      .then(async (response) => response.ok ? response.json() as Promise<{ authenticated?: unknown }> : null)
      .then((result) => setState(result?.authenticated === true ? "authenticated" : "guest"))
      .catch((error: unknown) => {
        // Burada farklı runtime'ların AbortError nesnelerini ortak name alanıyla ayırıp dev cleanup hatasını görünür duruma taşımıyorum.
        const isAbortError = Boolean(error && typeof error === "object" && "name" in error && error.name === "AbortError");
        if (!isAbortError) setState("guest");
      });

    return () => controller.abort();
  }, []);

  return <HeaderSessionContext value={state}>{children}</HeaderSessionContext>;
}

// Burada navbar ve ürün favori kontrollerinin aynı oturum snapshot'ını okumasına izin veriyorum.
export function useHeaderSession(): HeaderSessionState {
  return useContext(HeaderSessionContext);
}
