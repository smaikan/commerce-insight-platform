import Link from "next/link";
import type { CollectionPage } from "@/modules/collections/types";
import { CollectionDeleteButton } from "@/modules/collections/components/collection-delete-button";
import { CollectionThumbnail } from "@/modules/collections/components/collection-thumbnail";

// Burada koleksiyonları görsel, vitrin durumu, sıra ve güvenli işlemlerle hızlı taranabilir biçimde sunuyorum.
export function CollectionTable({ page }: { page: CollectionPage }) {
  if (page.items.length === 0) {
    return <div className="px-5 py-14 text-center"><h2 className="text-base font-semibold text-foreground">Henüz koleksiyon bulunmuyor</h2><p className="mt-2 text-sm text-muted">Yeni koleksiyonlar eklendiğinde burada listelenecek.</p></div>;
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[720px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr><th scope="col" className="w-[36%] px-4 py-2.5">Koleksiyon</th><th scope="col" className="px-3 py-2.5">URL</th><th scope="col" className="px-3 py-2.5">Durum</th><th scope="col" className="px-3 py-2.5">Vitrin</th><th scope="col" className="px-3 py-2.5 text-right">Sıra</th><th scope="col" className="px-4 py-2.5 text-right">İşlemler</th></tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((collection) => (
            <tr key={collection.id} className="hover:bg-primary-soft/20">
              <td className="px-4 py-2.5"><div className="flex min-w-0 items-center gap-3"><CollectionThumbnail src={collection.imageUrl} name={collection.name} /><div className="min-w-0"><p className="truncate font-semibold text-foreground">{collection.name}</p>{collection.description ? <p className="mt-0.5 max-w-xl truncate text-xs text-muted">{collection.description}</p> : <p className="mt-0.5 text-xs text-muted">Açıklama yok</p>}</div></div></td>
              <td className="px-3 py-3"><span className="block max-w-48 truncate text-muted">{collection.url || "—"}</span></td>
              <td className="px-3 py-3"><Badge active={collection.isActive} activeLabel="Aktif" inactiveLabel="Pasif" /></td>
              <td className="px-3 py-3"><Badge active={collection.isFeatured} activeLabel="Öne çıkarılmış" inactiveLabel="Standart" /></td>
              <td className="px-3 py-3 text-right font-semibold tabular-nums text-foreground">{collection.displayOrder}</td>
              <td className="px-4 py-3"><div className="flex justify-end gap-2"><Link href={`/collections/${encodeURIComponent(collection.id)}`} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle">Düzenle</Link><CollectionDeleteButton id={collection.id} name={collection.name} /></div></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Burada koleksiyon boolean alanlarını yalnız gerçek durum rozetleriyle ayırt ediyorum.
function Badge({ active, activeLabel, inactiveLabel }: { active: boolean; activeLabel: string; inactiveLabel: string }) {
  return <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-semibold ${active ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-slate-200 bg-slate-50 text-slate-700"}`}>{active ? activeLabel : inactiveLabel}</span>;
}
