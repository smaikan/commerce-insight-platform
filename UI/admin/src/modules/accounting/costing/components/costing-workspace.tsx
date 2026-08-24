import Link from "next/link";
import type { CostingQuery, CostingVariantOption, OpeningBalanceCostLayer, ProductVariantCostHistory } from "../types";
import { buildCostingHref } from "../query";
import { OpeningCostEditor } from "./opening-cost-editor";
import { CostHistory } from "./cost-history";

type Props = { query: CostingQuery; options: CostingVariantOption[]; selected: CostingVariantOption | null; layer: OpeningBalanceCostLayer | null; history: ProductVariantCostHistory[]; truncated: boolean };

// Burada maliyet modülünü ürün listesi görünümünden ayırıp seçim, katman düzenleme ve denetim izi şeklinde üç parçalı kuruyorum.
export function CostingWorkspace({ query, options, selected, layer, history, truncated }: Props) {
  return (
    <div className="grid gap-5 xl:grid-cols-[22rem_minmax(0,1fr)]">
      <aside className="self-start overflow-hidden rounded-xl border border-border bg-surface xl:sticky xl:top-4">
        <div className="border-b border-border p-4"><h2 className="font-semibold">Maliyet varyantı seçin</h2><p className="mt-1 text-sm leading-5 text-muted">SKU, ürün veya varyant değeriyle arayın.</p><form action="/accounting/costing" className="mt-3 flex gap-2"><label className="sr-only" htmlFor="costing-search">Varyant ara</label><input id="costing-search" name="search" type="search" maxLength={100} defaultValue={query.search} placeholder="Örn. SKU-001" className="min-h-10 min-w-0 flex-1 rounded-lg border border-border-strong px-3 text-sm" /><button className="min-h-10 cursor-pointer rounded-lg bg-primary px-3 text-sm font-semibold text-white hover:bg-primary-hover">Ara</button></form>{truncated ? <p className="mt-2 text-xs text-warning">İlk 20 ürün gösteriliyor; daha dar bir arama yazın.</p> : null}</div>
        <nav aria-label="Maliyet varyantları" className="max-h-[34rem] overflow-y-auto">{options.length ? options.map((option) => { const active = option.id === selected?.id; return <Link key={option.id} href={buildCostingHref({ search: query.search, productVariantId: option.id })} aria-current={active ? "page" : undefined} className={`block border-b border-border/80 px-4 py-3 last:border-b-0 ${active ? "bg-primary-soft/60" : "hover:bg-primary-soft/20"}`}><span className="flex items-center justify-between gap-3"><strong className="min-w-0 truncate text-sm">{option.productName}</strong><span className="font-mono text-xs text-muted">{option.sku}</span></span><span className="mt-1 block text-xs text-muted">{option.variantName} · Stok {option.stock}</span></Link>; }) : <p className="p-5 text-sm text-muted">Aramanızla eşleşen varyant bulunamadı.</p>}</nav>
      </aside>

      <div className="min-w-0 space-y-5">
        {!selected ? <section className="rounded-xl border border-dashed border-border-strong bg-surface px-6 py-16 text-center"><p className="text-xs font-bold uppercase tracking-[0.14em] text-primary">FIFO maliyet çalışma alanı</p><h2 className="mt-2 text-xl font-semibold">Bir varyant seçerek başlayın</h2><p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-muted">Açılış maliyetini düzeltmek ve maliyet geçmişini incelemek için soldaki sicilden bir varyant seçin.</p></section> : <><section className="rounded-xl border border-slate-800 bg-slate-900 p-5 text-white"><div className="flex flex-wrap items-start justify-between gap-4"><div><p className="text-xs font-bold uppercase tracking-[0.14em] text-sky-300">Seçili maliyet kartı</p><h1 className="mt-1 text-xl font-semibold">{selected.productName}</h1><p className="mt-1 text-sm text-slate-300">{selected.variantName}</p></div><div className="text-right"><p className="font-mono text-sm font-semibold">{selected.sku}</p><p className="mt-1 text-xs text-slate-300">Fiziksel stok: {selected.stock}</p></div></div></section>{layer ? <OpeningCostEditor layer={layer} /> : <section className="rounded-xl border border-amber-300 bg-amber-50 p-5 text-amber-950"><h2 className="font-semibold">Düzenlenebilir açılış katmanı yok</h2><p className="mt-1 text-sm leading-6">Bu varyantın açılış stok katmanı bulunmadığı için maliyet değiştirilemez. Satın alma kaynaklı maliyetler belge yaşam döngüsünden yönetilir.</p></section>}<CostHistory history={history} /></>}
      </div>
    </div>
  );
}
