"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { formatAccountingMoney } from "@/modules/accounting/core/presentation";
import { addPurchaseInvoiceExpenseAction } from "../actions";
import { expenseAllocationMethodLabel } from "../presentation";
import type { AccountingFormState, ExpenseCategory, PurchaseInvoice, PurchaseInvoiceExpense, PurchaseInvoiceExpenseDraft } from "../types";

type InvoiceLine = PurchaseInvoice["lines"][number];

export function PurchaseInvoiceExpenses({ invoiceId, currencyCode, status, lines, expenses, categories }: { invoiceId: string; currencyCode: string; status: number; lines: InvoiceLine[]; expenses: PurchaseInvoiceExpense[]; categories: ExpenseCategory[] }) {
  const categoryById = new Map(categories.map((category) => [category.id, category]));
  return (
    <section className="rounded-xl border border-border bg-surface">
      <div className="border-b border-border px-4 py-4 sm:px-5"><h2 className="text-base font-semibold">Alış faturası giderleri</h2><p className="mt-1 text-sm text-muted">Nakliye ve benzeri giderler satırlara dağıtılır; final maliyet yalnız API yanıtından gösterilir.</p></div>
      {expenses.length ? <ul className="divide-y divide-border">{expenses.map((expense) => <li key={expense.id} className="px-4 py-4 sm:px-5"><div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between"><div><p className="font-semibold">{categoryById.get(expense.categoryId)?.name ?? `Kategori ${expense.categoryId.slice(0, 8)}…`}</p><p className="mt-1 text-xs text-muted">{expenseAllocationMethodLabel(expense.allocationMethod)} · {expense.allocations.length} satıra dağıtıldı</p></div><div className="text-left sm:text-right"><p className="font-semibold tabular-nums">{formatAccountingMoney(expense.amountIncludingVat, currencyCode)}</p><p className="text-xs text-muted">KDV hariç {formatAccountingMoney(expense.amountExcludingVat, currencyCode)}</p></div></div><div className="mt-3 flex flex-wrap gap-2">{expense.allocations.map((allocation) => { const line = lines.find((item) => item.id === allocation.lineId); return <span key={allocation.lineId} className="rounded-md border border-border bg-surface-subtle px-2 py-1 text-xs">Satır {line?.lineNumber ?? "?"}: {formatAccountingMoney(allocation.amountExcludingVat, currencyCode)}</span>; })}</div></li>)}</ul> : <p className="px-5 py-8 text-center text-sm text-muted">Bu faturaya henüz gider dağıtılmadı.</p>}
      {status === 1 ? <div className="border-t border-border p-4 sm:p-5"><PurchaseInvoiceExpenseForm invoiceId={invoiceId} lines={lines} categories={categories.filter((category) => category.isActive)} /></div> : null}
    </section>
  );
}

// Burada manuel yöntemde fatura satırlarının her birini tek kez taşıyan kontrollü gider formu kuruyorum.
function PurchaseInvoiceExpenseForm({ invoiceId, lines, categories }: { invoiceId: string; lines: InvoiceLine[]; categories: ExpenseCategory[] }) {
  const router = useRouter();
  const guard = useRef(false);
  const [method, setMethod] = useState("1");
  const [manual, setManual] = useState<Record<string, string>>(() => Object.fromEntries(lines.map((line) => [line.id, "0"])));
  const action = addPurchaseInvoiceExpenseAction.bind(null, invoiceId, lines.map((line) => line.id));
  const [state, formAction, pending] = useActionState<AccountingFormState<PurchaseInvoiceExpenseDraft>, FormData>(action, { status: "idle" });
  const manualJson = lines.map((line) => ({ purchaseInvoiceLineId: line.id, amountExcludingVat: manual[line.id] ?? "0" }));

  useEffect(() => {
    if (state.refresh) router.refresh();
    guard.current = false;
  }, [router, state]);

  return <form action={formAction} onSubmit={(event) => { if (guard.current) event.preventDefault(); else guard.current = true; }} aria-busy={pending} className="space-y-4">
    <input type="hidden" name="manualAllocationsJson" value={JSON.stringify(manualJson)} />
    <div><h3 className="text-sm font-semibold">Gider ekle</h3><p className="mt-1 text-xs leading-5 text-muted">Kayıt append-only’dir; mevcut API güncelleme veya silme sunmaz. Bilinmeyen ağ sonucunda otomatik tekrar gönderilmez.</p></div>
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <label className="text-xs font-semibold text-muted">Kategori *<select name="categoryId" required defaultValue={state.draft?.categoryId ?? ""} className={inputClass}><option value="">Kategori seçin</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.code} — {category.name}</option>)}</select><ErrorText text={error(state, "categoryId")} /></label>
      <label className="text-xs font-semibold text-muted">Dağıtım yöntemi *<select name="allocationMethod" value={method} onChange={(event) => setMethod(event.target.value)} className={inputClass}><option value="1">KDV hariç satır tutarı</option><option value="2">Stok miktarı</option><option value="3">Manuel dağıtım</option></select></label>
      <label className="text-xs font-semibold text-muted">KDV hariç tutar *<input name="amountExcludingVat" type="number" inputMode="decimal" min="0.01" step="0.01" required defaultValue={state.draft?.amountExcludingVat ?? ""} className={inputClass} /><ErrorText text={error(state, "amountExcludingVat")} /></label>
      <label className="text-xs font-semibold text-muted">KDV oranı (%) *<input name="vatRate" type="number" inputMode="decimal" min="0" max="100" step="0.01" required defaultValue={state.draft?.vatRate ?? "20"} className={inputClass} /><ErrorText text={error(state, "vatRate")} /></label>
    </div>
    <label className="block text-xs font-semibold text-muted">Açıklama <span className="font-normal">(response DTO geçmişte geri döndürmez)</span><input name="description" maxLength={500} defaultValue={state.draft?.description ?? ""} className={inputClass} /><ErrorText text={error(state, "description")} /></label>
    {method === "3" ? <fieldset className="rounded-lg border border-border bg-surface-subtle/40 p-3"><legend className="px-1 text-sm font-semibold">Manuel satır dağıtımı</legend><div className="mt-1 grid gap-3 sm:grid-cols-2">{lines.map((line, index) => <label key={line.id} className="text-xs font-semibold text-muted">Satır {line.lineNumber} · {line.productName}<input type="number" inputMode="decimal" min="0" step="0.01" value={manual[line.id] ?? "0"} onChange={(event) => setManual({ ...manual, [line.id]: event.target.value })} className={inputClass} /><ErrorText text={error(state, `manualAllocations.${index}.amountExcludingVat`, "manualAllocations")} /></label>)}</div></fieldset> : null}
    {state.status !== "idle" ? <p role={state.status === "error" ? "alert" : "status"} className={`rounded-lg border px-3 py-2 text-sm ${state.status === "error" ? "border-red-200 bg-red-50 text-red-950" : "border-emerald-200 bg-emerald-50 text-emerald-950"}`}>{state.message}{state.traceId ? <span className="mt-1 block font-mono text-xs">Takip: {state.traceId}</span> : null}</p> : null}
    <div className="flex justify-end"><button type="submit" disabled={pending || categories.length === 0} className="min-h-10 cursor-pointer rounded-lg border border-primary bg-surface px-4 text-sm font-semibold text-primary hover:bg-primary-soft/40 disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Gider dağıtılıyor…" : "Gideri dağıt"}</button></div>
  </form>;
}

const inputClass = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-foreground outline-none focus:ring-2 focus:ring-focus";
function ErrorText({ text }: { text?: string }) { return text ? <span className="mt-1 block text-xs font-semibold text-danger">{text}</span> : null; }
function error(state: AccountingFormState<PurchaseInvoiceExpenseDraft>, ...keys: string[]): string | undefined { for (const key of keys) { const message = state.fieldErrors?.[key]?.[0]; if (message) return message; } return undefined; }
