"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { cancelPaymentAction } from "../actions";
import type { Payment } from "../types";
import { initialPaymentFormState } from "../types";

export function PaymentLifecycleActions({ payment }: { payment: Payment }) {
  const router = useRouter(); const dialog = useRef<HTMLDialogElement>(null); const [open, setOpen] = useState(false);
  const [state, action, pending] = useActionState(cancelPaymentAction.bind(null, payment.id, payment.currentAccountId), initialPaymentFormState);
  useEffect(() => { if (open) dialog.current?.showModal(); else dialog.current?.close(); }, [open]);
  useEffect(() => { if (state.refresh) { dialog.current?.close(); router.refresh(); } }, [router, state.refresh]);
  return <section className="rounded-xl border border-danger/20 bg-surface p-5"><h2 className="font-semibold">Yaşam döngüsü</h2><p className="mt-2 text-sm leading-6 text-muted">İptal, dağıtımları ve bağlı cari/kasa/banka etkilerini ters kayıtla kapatır; özgün kayıt silinmez.</p><button type="button" onClick={() => setOpen(true)} className="mt-4 min-h-10 cursor-pointer rounded-lg border border-danger px-4 text-sm font-semibold text-danger hover:bg-red-50">Ödemeyi iptal et</button>{state.status === "error" ? <p role="alert" className="mt-3 text-sm text-danger">{state.message}</p> : null}<dialog ref={dialog} onClose={() => setOpen(false)} className="m-auto w-[min(32rem,calc(100%-2rem))] rounded-xl border border-border bg-surface p-0 text-foreground shadow-xl backdrop:bg-slate-950/40"><form action={action} className="p-5"><h3 className="text-lg font-semibold">Ödemeyi iptal et</h3><p className="mt-2 text-sm text-muted">Bu işlem ters kayıt üretir ve açık kalem bakiyelerini yeniden hesaplatır.</p><label className="mt-4 block text-sm font-medium">İptal gerekçesi *<textarea name="reason" required maxLength={500} autoFocus rows={4} className="mt-1.5 w-full rounded-lg border border-border-strong px-3 py-2" /></label><div className="mt-5 flex justify-end gap-2"><button type="button" onClick={() => setOpen(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong px-4 text-sm font-semibold">Vazgeç</button><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-danger px-4 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "İptal ediliyor…" : "İptali onayla"}</button></div></form></dialog></section>;
}
