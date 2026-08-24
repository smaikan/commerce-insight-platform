"use client";

import { useActionState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { createExpenseCategoryAction, createGeneralExpenseAction } from "../actions";
import type { AccountingFormState, ExpenseCategory, ExpenseCategoryDraft, GeneralExpenseDraft } from "../types";

export function GeneralExpenseForm({ categories }: { categories: ExpenseCategory[] }) {
  const router = useRouter();
  const guard = useRef(false);
  const [state, action, pending] = useActionState<AccountingFormState<GeneralExpenseDraft>, FormData>(createGeneralExpenseAction, { status: "idle" });
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="rounded-xl border border-border bg-surface p-4 sm:p-5" aria-busy={pending}>
    <div className="border-b border-border pb-4"><h2 className="text-base font-semibold">Genel gider kaydı</h2><p className="mt-1 text-sm text-muted">Stok maliyetini değiştirmeyen append-only operasyon gideri.</p></div>
    <div className="mt-4 grid gap-4 sm:grid-cols-2">
      <label className="text-sm font-medium sm:col-span-2">Kategori *<select name="categoryId" required defaultValue={state.draft?.categoryId ?? ""} className={inputClass}><option value="">Aktif kategori seçin</option>{categories.filter((category) => category.isActive).map((category) => <option key={category.id} value={category.id}>{category.code} — {category.name}</option>)}</select><Error text={fieldError(state, "categoryId")} /></label>
      <label className="text-sm font-medium">KDV hariç tutar *<input name="amountExcludingVat" type="number" inputMode="decimal" min="0.01" step="0.01" required defaultValue={state.draft?.amountExcludingVat ?? ""} className={inputClass} /><Error text={fieldError(state, "amountExcludingVat")} /></label>
      <label className="text-sm font-medium">KDV oranı (%) *<input name="vatRate" type="number" inputMode="decimal" min="0" max="100" step="0.01" required defaultValue={state.draft?.vatRate ?? "20"} className={inputClass} /><Error text={fieldError(state, "vatRate")} /></label>
      <label className="text-sm font-medium">Gider tarihi *<input name="expenseDate" type="date" required defaultValue={state.draft?.expenseDate ?? new Date().toISOString().slice(0, 10)} className={inputClass} /><Error text={fieldError(state, "expenseDate")} /></label>
      <label className="text-sm font-medium sm:col-span-2">Açıklama *<textarea name="description" rows={3} required maxLength={500} defaultValue={state.draft?.description ?? ""} className={`${inputClass} py-2`} /><Error text={fieldError(state, "description")} /></label>
    </div>
    <p className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs leading-5 text-amber-950">Bu kayıt mevcut API ile sonradan düzenlenemez, silinemez, post veya iptal edilemez. Ağ sonucu belirsizse otomatik tekrar gönderilmez.</p>
    <ActionState state={state} />
    <div className="mt-4 flex justify-end"><button type="submit" disabled={pending || categories.length === 0} className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : "Genel gideri kaydet"}</button></div>
  </form>;
}

export function ExpenseCategoryForm() {
  const router = useRouter();
  const guard = useRef(false);
  const [state, action, pending] = useActionState<AccountingFormState<ExpenseCategoryDraft>, FormData>(createExpenseCategoryAction, { status: "idle" });
  useEffect(() => { if (state.refresh) router.refresh(); guard.current = false; }, [router, state]);
  return <form action={action} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} className="rounded-xl border border-border bg-surface p-4 sm:p-5" aria-busy={pending}>
    <div className="border-b border-border pb-4"><h2 className="text-base font-semibold">Gider kategorisi oluştur</h2><p className="mt-1 text-sm text-muted">Genel gider ve alış faturası giderlerinde kullanılacak sınıflandırma.</p></div>
    <div className="mt-4 grid gap-4 sm:grid-cols-2"><label className="text-sm font-medium">Kategori kodu *<input name="code" required maxLength={50} defaultValue={state.draft?.code ?? ""} className={inputClass} /><Error text={fieldError(state, "code")} /></label><label className="text-sm font-medium">Kategori adı *<input name="name" required maxLength={150} defaultValue={state.draft?.name ?? ""} className={inputClass} /><Error text={fieldError(state, "name")} /></label></div>
    <p className="mt-4 text-xs leading-5 text-muted">Kod uppercase normalize edilir ve benzersiz olmalıdır. Mevcut API kategori güncelleme, pasife alma veya silme işlemi sunmaz.</p>
    <ActionState state={state} />
    <div className="mt-4 flex justify-end"><button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Oluşturuluyor…" : "Kategori oluştur"}</button></div>
  </form>;
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm outline-none focus:ring-2 focus:ring-focus";
function Error({ text }: { text?: string }) { return text ? <span className="mt-1 block text-xs font-semibold text-danger">{text}</span> : null; }
function fieldError<T>(state: AccountingFormState<T>, key: string): string | undefined { return state.fieldErrors?.[key]?.[0]; }
function ActionState<T>({ state }: { state: AccountingFormState<T> }) { return state.status === "idle" ? null : <p role={state.status === "error" ? "alert" : "status"} className={`mt-4 rounded-lg border p-3 text-sm ${state.status === "error" ? "border-red-200 bg-red-50 text-red-950" : "border-emerald-200 bg-emerald-50 text-emerald-950"}`}>{state.message}{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip: {state.traceId}</span> : null}</p>; }
