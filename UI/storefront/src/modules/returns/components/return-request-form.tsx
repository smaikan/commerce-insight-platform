"use client";

import { useActionState, useState } from "react";

import { createReturnAction } from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import type { AccountOrder, ProductVariant, AccountActionState } from "@/modules/account/contracts";
import { INITIAL_ACCOUNT_ACTION_STATE } from "@/modules/account/contracts";

// Burada seçilen talep türüne göre adet ve uygun replacement varyantlarını aynı formda yönetiyorum.
export function ReturnRequestForm({ order, variants }: { order: AccountOrder; variants: Record<string, ProductVariant[]> }) {
  const [type, setType] = useState<"0" | "1">("0");
  const action = createReturnAction.bind(null, order.id);
  const [state, formAction, pending] = useActionState<AccountActionState, FormData>(action, INITIAL_ACCOUNT_ACTION_STATE);

  return <form action={formAction} className="mt-6 space-y-6">
    <fieldset className="border border-line bg-surface p-5"><legend className="px-2 text-sm font-black text-ink">Talep türü</legend><div className="flex flex-wrap gap-5"><label className="flex min-h-11 items-center gap-2 text-sm font-bold"><input type="radio" name="type" value="0" checked={type === "0"} onChange={() => setType("0")} /> İade</label><label className="flex min-h-11 items-center gap-2 text-sm font-bold"><input type="radio" name="type" value="1" checked={type === "1"} onChange={() => setType("1")} /> Değişim</label></div></fieldset>
    <fieldset className="border border-line bg-surface"><legend className="sr-only">Sipariş ürünleri</legend><ul className="divide-y divide-line">{order.items.map((item) => {
      const eligible = (variants[item.productId] || []).filter((variant) => variant.id !== item.productVariantId && variant.isActive && variant.stock > 0 && variant.netPrice === item.unitPrice);
      return <li key={item.id} className="grid gap-4 p-5 md:grid-cols-[minmax(0,1fr)_8rem_minmax(12rem,0.8fr)] md:items-end"><input type="hidden" name="orderItemId" value={item.id} /><div><strong className="text-sm text-ink">{item.productTitle}</strong><p className="mt-1 text-xs text-ink-muted">{item.variantName && item.variantValue ? `${item.variantName}: ${item.variantValue}` : `SKU: ${item.variantSku}`} · Siparişte {item.quantity} adet</p></div><label className="text-xs font-bold text-ink-muted">İade adedi<select name={`quantity:${item.id}`} defaultValue="0" className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm text-ink"><option value="0">Seçme</option>{Array.from({ length: item.quantity }, (_, index) => <option key={index + 1} value={index + 1}>{index + 1}</option>)}</select></label>{type === "1" ? <label className="text-xs font-bold text-ink-muted">Yeni varyant<select name={`replacement:${item.id}`} defaultValue="" className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm text-ink"><option value="">Varyant seçin</option>{eligible.map((variant) => <option key={variant.id} value={variant.id}>{variant.name}: {variant.value}</option>)}</select>{eligible.length === 0 ? <span className="mt-1 block font-normal text-danger">Uygun stoklu varyant yok.</span> : null}</label> : <span />}</li>;
    })}</ul></fieldset>
    <label className="grid gap-2 text-sm font-black text-ink">Talep notu <textarea name="customerNote" maxLength={1000} rows={4} className="focus-ring border border-line bg-surface p-3 text-sm font-normal" /></label>
    <ActionFeedback state={state} />
    <button disabled={pending} className="focus-ring min-h-12 bg-brand-950 px-6 text-sm font-bold text-white hover:bg-brand-700 disabled:opacity-60">{pending ? "Talep oluşturuluyor…" : `${type === "1" ? "Değişim" : "İade"} talebi oluştur`}</button>
  </form>;
}
