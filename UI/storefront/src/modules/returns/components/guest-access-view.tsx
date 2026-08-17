"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import { exchangeGuestAccessToken, requestGuestAccessLink } from "@/modules/returns/client";

// Burada e-postadaki fragment tokenını sunucu loglarına taşımadan exchange ediyor, token yoksa eşit cevaplı erişim formunu gösteriyorum.
export function GuestAccessView() {
  const router = useRouter();
  const [exchangeState, setExchangeState] = useState<"checking" | "form" | "error">("checking");
  const [message, setMessage] = useState("");
  const [pending, setPending] = useState(false);

  useEffect(() => {
    const token = new URLSearchParams(window.location.hash.slice(1)).get("token");
    if (!token) { queueMicrotask(() => setExchangeState("form")); return; }
    window.history.replaceState(null, "", window.location.pathname);
    void exchangeGuestAccessToken(token).then((result) => router.replace(`/guest-orders/${result.orderId}/returns`)).catch(() => setExchangeState("error"));
  }, [router]);

  async function submit(formData: FormData) {
    setPending(true); setMessage("");
    try {
      await requestGuestAccessLink(String(formData.get("orderNumber") || "").trim(), String(formData.get("email") || "").trim());
      setMessage("Bilgiler eşleşiyorsa erişim bağlantısı e-posta adresinize gönderildi.");
    } catch { setMessage("İstek şu anda tamamlanamadı. Lütfen kısa bir süre sonra tekrar deneyin."); }
    setPending(false);
  }

  if (exchangeState === "checking") return <p className="text-sm text-ink-muted" aria-busy="true">Erişim doğrulanıyor…</p>;
  if (exchangeState === "error") return <section><h1 className="text-2xl font-black text-ink">Bağlantı kullanılamadı</h1><p className="mt-3 text-sm text-ink-muted">Bağlantının süresi dolmuş veya daha önce kullanılmış olabilir. Yeni bir erişim bağlantısı isteyin.</p><button onClick={() => setExchangeState("form")} className="focus-ring mt-5 min-h-11 bg-brand-950 px-5 text-sm font-bold text-white">Yeni bağlantı iste</button></section>;
  return <section><p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Misafir sipariş işlemleri</p><h1 className="mt-3 text-3xl font-black text-ink">Siparişime eriş</h1><p className="mt-3 text-sm leading-6 text-ink-muted">Sipariş numaranız ve alışverişte kullandığınız e-posta adresiyle güvenli, tek kullanımlık bağlantı isteyin.</p><form action={submit} className="mt-6 grid gap-4"><label className="grid gap-2 text-sm font-bold">Sipariş numarası<input required name="orderNumber" autoComplete="off" className="focus-ring min-h-11 border border-line px-3 font-normal" /></label><label className="grid gap-2 text-sm font-bold">E-posta adresi<input required type="email" name="email" autoComplete="email" className="focus-ring min-h-11 border border-line px-3 font-normal" /></label><button disabled={pending} className="focus-ring min-h-12 bg-brand-950 px-6 text-sm font-bold text-white disabled:opacity-60">{pending ? "Gönderiliyor…" : "Erişim bağlantısı gönder"}</button></form>{message ? <p role="status" className="mt-4 border border-line bg-surface-subtle p-4 text-sm leading-6 text-ink">{message}</p> : null}</section>;
}
