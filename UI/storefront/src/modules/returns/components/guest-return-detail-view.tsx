"use client";

import { useEffect, useState } from "react";

import type { AccountReturn } from "@/modules/account/contracts";
import { getGuestReturn } from "@/modules/returns/client";
import { ReturnDetail } from "@/modules/returns/components/return-detail";

// Burada misafir talep detayını yalnız session grant ile browser üzerinden okuyup no-store görünümüne taşıyorum.
export function GuestReturnDetailView({ orderId, returnId }: { orderId: string; returnId: string }) {
  const [value, setValue] = useState<AccountReturn | null>(null);
  const [error, setError] = useState("");
  useEffect(() => { let active = true; void getGuestReturn(orderId, returnId).then((result) => { if (active) setValue(result); }).catch((reason) => { if (active) setError(reason instanceof Error ? reason.message : "Talep açılamadı."); }); return () => { active = false; }; }, [orderId, returnId]);
  if (error) return <main id="main-content" className="page-shell flex-1 py-16"><p role="alert" className="text-sm text-danger">{error}</p></main>;
  if (!value) return <main id="main-content" className="page-shell flex-1 py-16" aria-busy="true"><p className="text-sm text-ink-muted">Talep yükleniyor…</p></main>;
  return <main id="main-content" className="page-shell max-w-[64rem] flex-1 py-10 sm:py-14"><ReturnDetail value={value} backHref={`/guest-orders/${orderId}/returns`} /></main>;
}
