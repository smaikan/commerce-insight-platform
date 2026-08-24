"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { reverseFinancialTransactionAction } from "../actions";
import { initialTreasuryFormState } from "../types";

export function TransactionLifecycleAction({ transactionId, accountPath }: { transactionId: string; accountPath: string }) {
  const router = useRouter(); const dialog = useRef<HTMLDialogElement>(null); const [open, setOpen] = useState(false);
  const [state, action, pending] = useActionState(reverseFinancialTransactionAction.bind(null, transactionId, accountPath), initialTreasuryFormState);
  useEffect(() => { if (open) dialog.current?.showModal(); else dialog.current?.close(); }, [open]);
  useEffect(() => { if (state.refresh) { dialog.current?.close(); router.refresh(); } }, [router, state.refresh]);
  return <><button type="button" onClick={() => setOpen(true)} className="min-h-9 cursor-pointer rounded-lg border border-border-strong px-3 text-xs font-semibold hover:bg-surface-subtle">Ters kayıt</button><dialog ref={dialog} onClose={() => setOpen(false)} className="m-auto w-[min(30rem,calc(100%-2rem))] rounded-xl border border-border bg-surface p-0 shadow-xl backdrop:bg-slate-950/40"><form action={action} className="p-5"><h3 className="text-lg font-semibold">Ters kayıt oluştur</h3><p className="mt-2 text-sm text-muted">Özgün hareket korunur; karşı yönlü yeni hareket oluşturulur.</p>{state.status === "error" ? <p role="alert" className="mt-3 text-sm text-danger">{state.message}</p> : null}<label className="mt-4 block text-sm font-medium">Gerekçe *<textarea name="reason" required maxLength={500} autoFocus rows={3} className="mt-1.5 w-full rounded-lg border border-border-strong px-3 py-2" /></label><div className="mt-5 flex justify-end gap-2"><button type="button" onClick={() => setOpen(false)} className="min-h-10 cursor-pointer rounded-lg border border-border-strong px-4 text-sm font-semibold">Vazgeç</button><button disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Oluşturuluyor…" : "Ters kaydı onayla"}</button></div></form></dialog></>;
}
