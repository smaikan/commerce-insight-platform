"use client";

import Link from "next/link";
import { useActionState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { createBankAccountAction, createCashAccountAction } from "../actions";
import { initialTreasuryFormState } from "../types";

export function FinancialAccountForm({ kind }: { kind: "cash" | "bank" }) {
  const router = useRouter(); const alert = useRef<HTMLDivElement>(null); const submitGuard = useRef(false);
  const [state, action, pending] = useActionState(kind === "cash" ? createCashAccountAction : createBankAccountAction, initialTreasuryFormState);
  // Burada yeni finans hesabını ikinci bir refresh başlatmadan yetkili detay route'una taşıyorum.
  useEffect(() => { if (state.redirectHref) router.replace(state.redirectHref); }, [router, state.redirectHref]);
  useEffect(() => { if (state.status === "error") alert.current?.focus(); submitGuard.current = false; }, [state]);
  return <form action={action} onSubmit={(event) => { if (submitGuard.current) event.preventDefault(); else submitGuard.current = true; }} className="rounded-xl border border-border bg-surface p-5">{state.status === "error" ? <div ref={alert} role="alert" tabIndex={-1} className="mb-4 rounded-lg border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900"><strong>{state.message}</strong>{state.fieldErrors ? <ul className="mt-2 list-disc pl-5">{Object.values(state.fieldErrors).flat().map((message) => <li key={message}>{message}</li>)}</ul> : null}</div> : null}<div className="grid gap-4 sm:grid-cols-2"><Field name="code" label="Hesap kodu" required maxLength={50} /><Field name="name" label="Hesap adı" required maxLength={150} />{kind === "bank" ? <><Field name="bankName" label="Banka adı" required maxLength={150} /><Field name="iban" label="IBAN" maxLength={38} /></> : null}</div><div className="mt-5 rounded-lg border border-border bg-surface-subtle/50 px-4 py-3 text-sm text-muted">Para birimi TRY olarak oluşturulur. Açılış veya bakiye alanı yoktur; bakiye yalnız hareketlerden türetilir.</div><div className="mt-6 flex justify-end gap-2"><Link href="/accounting/treasury" className="inline-flex min-h-10 cursor-pointer items-center rounded-lg border border-border-strong px-4 text-sm font-semibold">Vazgeç</Link><button disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Oluşturuluyor…" : kind === "cash" ? "Kasayı oluştur" : "Banka hesabını oluştur"}</button></div></form>;
}
function Field(props: React.InputHTMLAttributes<HTMLInputElement> & { name: string; label: string }) { return <label className="text-sm font-medium">{props.label}{props.required ? " *" : ""}<input {...props} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong px-3" /></label>; }
