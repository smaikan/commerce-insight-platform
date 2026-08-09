"use client";

import { useActionState, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { createBulkStockMovementsAction } from "@/modules/inventory/actions";
import { manualStockMovementTypeOptions, stockMovementDirectionOptions } from "@/modules/inventory/stock-movement-rules";
import { initialStockMovementActionState } from "@/modules/inventory/types";

type DraftMovement = { key: number; productVariantId: string; type: number; direction: number; quantity: string; reason: string };

// Burada ilk hareket satırını izinli varsayılan satın alma girişiyle hazırlıyorum.
function initialRow(key: number): DraftMovement {
  return { key, productVariantId: "", type: 10, direction: 1, quantity: "", reason: "" };
}

// Burada atomik toplu hareket formunu, kullanıcı taslağını koruyan dar bir istemci sınırında yönetiyorum.
export function StockMovementForm() {
  const router = useRouter();
  const [rows, setRows] = useState<DraftMovement[]>([initialRow(1)]);
  const [state, formAction, isPending] = useActionState(createBulkStockMovementsAction, initialStockMovementActionState);

  // Burada başarılı API sonucundan sonra güncel deftere geri dönerek yetkili kaydı gösteriyorum.
  useEffect(() => {
    if (state.status === "success" && state.movementCount) router.push(`/inventory/stock-movements?created=${state.movementCount}`);
  }, [router, state.movementCount, state.status]);

  const updateRow = (key: number, patch: Partial<DraftMovement>) => setRows((current) => current.map((row) => row.key === key ? { ...row, ...patch } : row));
  const addRow = () => setRows((current) => current.length >= 500 ? current : [...current, initialRow(Math.max(...current.map((row) => row.key)) + 1)]);
  const removeRow = (key: number) => setRows((current) => current.length === 1 ? current : current.filter((row) => row.key !== key));

  return (
    <form action={formAction} className="space-y-5">
      <input type="hidden" name="movements" value={JSON.stringify(rows)} />
      {state.status === "error" ? <div role="alert" className="rounded-xl border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger"><p className="font-semibold">Hareketler kaydedilemedi</p><p className="mt-1">{state.message}</p>{state.traceId ? <p className="mt-2 font-mono text-xs">Takip: {state.traceId}</p> : null}</div> : null}
      <section aria-labelledby="movement-lines-title" className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="flex flex-col gap-3 border-b border-border bg-surface-subtle px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
          <div><h2 id="movement-lines-title" className="text-base font-semibold text-foreground">Hareket satırları</h2><p className="mt-1 text-sm text-muted">Aynı anda en fazla 500 hareket kaydedilir; işlem ya tamamen uygulanır ya da hiç uygulanmaz.</p></div>
          <button type="button" onClick={addRow} disabled={rows.length >= 500 || isPending} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">Satır ekle</button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[970px] border-collapse text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/60 text-[11px] font-bold uppercase tracking-[0.08em] text-muted"><tr><th className="px-4 py-2.5">Varyant kimliği</th><th className="px-3 py-2.5">Tür</th><th className="px-3 py-2.5">Yön</th><th className="px-3 py-2.5">Miktar</th><th className="px-3 py-2.5">Açıklama</th><th className="w-12 px-3 py-2.5"><span className="sr-only">Satırı kaldır</span></th></tr></thead>
            <tbody className="divide-y divide-border/80">
              {rows.map((row, index) => {
                const typeOption = manualStockMovementTypeOptions.find((option) => option.value === row.type) || manualStockMovementTypeOptions[0];
                const allowedDirections = typeOption.allowedDirections;
                return <tr key={row.key} className="align-top"><td className="px-4 py-3"><label className="sr-only" htmlFor={`variant-${row.key}`}>{index + 1}. satır varyant kimliği</label><input id={`variant-${row.key}`} name={`variant-${row.key}`} type="text" value={row.productVariantId} onChange={(event) => updateRow(row.key, { productVariantId: event.target.value })} required disabled={isPending} placeholder="UUID" className="min-h-10 w-72 rounded-lg border border-border-strong bg-surface-strong px-3 font-mono text-xs text-foreground outline-none placeholder:font-sans placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30" /><p className="mt-1 text-xs text-muted">Ürün düzenleme ekranındaki varyant kimliği</p></td><td className="px-3 py-3"><label className="sr-only" htmlFor={`type-${row.key}`}>{index + 1}. satır hareket türü</label><select id={`type-${row.key}`} value={row.type} onChange={(event) => { const type = Number(event.target.value); const option = manualStockMovementTypeOptions.find((item) => item.value === type); updateRow(row.key, { type, direction: option?.allowedDirections[0] || 1 }); }} disabled={isPending} className="min-h-10 min-w-44 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30">{manualStockMovementTypeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></td><td className="px-3 py-3"><label className="sr-only" htmlFor={`direction-${row.key}`}>{index + 1}. satır yönü</label><select id={`direction-${row.key}`} value={row.direction} onChange={(event) => updateRow(row.key, { direction: Number(event.target.value) })} disabled={isPending} className="min-h-10 min-w-28 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30">{stockMovementDirectionOptions.filter((option) => allowedDirections.includes(option.value)).map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></td><td className="px-3 py-3"><label className="sr-only" htmlFor={`quantity-${row.key}`}>{index + 1}. satır miktarı</label><input id={`quantity-${row.key}`} name={`quantity-${row.key}`} type="number" inputMode="numeric" min="1" step="1" value={row.quantity} onChange={(event) => updateRow(row.key, { quantity: event.target.value })} required disabled={isPending} className="min-h-10 w-24 rounded-lg border border-border-strong bg-surface-strong px-3 text-right tabular-nums text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30" /></td><td className="px-3 py-3"><label className="sr-only" htmlFor={`reason-${row.key}`}>{index + 1}. satır açıklaması</label><input id={`reason-${row.key}`} name={`reason-${row.key}`} type="text" maxLength={500} value={row.reason} onChange={(event) => updateRow(row.key, { reason: event.target.value })} disabled={isPending} placeholder="Opsiyonel açıklama" className="min-h-10 w-64 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30" /></td><td className="px-3 py-3"><button type="button" onClick={() => removeRow(row.key)} disabled={rows.length === 1 || isPending} aria-label={`${index + 1}. hareket satırını kaldır`} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-sm font-semibold text-muted hover:border-danger/35 hover:text-danger disabled:cursor-not-allowed disabled:opacity-50">Kaldır</button></td></tr>;
              })}
            </tbody>
          </table>
        </div>
      </section>
      <section aria-labelledby="movement-rule-title" className="rounded-xl border border-border bg-surface px-4 py-4 sm:px-5"><h2 id="movement-rule-title" className="text-base font-semibold text-foreground">İşlem kuralları</h2><ul className="mt-2 space-y-1.5 text-sm leading-6 text-muted"><li>Satış, iptal ve iade gibi sistem kaynaklı hareketler bu formdan oluşturulamaz.</li><li>Miktar pozitif girilir; seçilen yön API’ye imzalı hareket olarak gönderilir.</li><li>Stok eksiye düşerse veya satırlardan biri geçersizse API tüm işlemi reddeder; kısmi kayıt oluşmaz.</li></ul></section>
      <div className="flex flex-col-reverse gap-3 border-t border-border pt-4 sm:flex-row sm:items-center sm:justify-between"><p className="text-sm text-muted">{rows.length} / 500 hareket satırı</p><div className="flex flex-wrap gap-2"><a href="/inventory/stock-movements" className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</a><button type="submit" disabled={isPending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{isPending ? "Kaydediliyor…" : "Hareketleri kaydet"}</button></div></div>
    </form>
  );
}
