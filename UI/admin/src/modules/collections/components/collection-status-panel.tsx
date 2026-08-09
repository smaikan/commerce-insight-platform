import { setCollectionActivationAction, setCollectionFeaturedAction } from "@/modules/collections/actions";
import type { Collection } from "@/modules/collections/types";

// Burada koleksiyonun aktiflik ve vitrin durumlarını birbirinden bağımsız, açık eylemlerle yönetiyorum.
export function CollectionStatusPanel({ collection }: { collection: Collection }) {
  return (
    <section className="mt-5 rounded-xl border border-border bg-surface-strong" aria-labelledby="collection-status-title">
      <div className="border-b border-border px-4 py-3 sm:px-5">
        <h2 id="collection-status-title" className="text-base font-semibold text-foreground">Yayın durumu</h2>
        <p className="mt-1 text-sm text-muted">Koleksiyonun kullanılabilirliğini ve vitrindeki görünürlüğünü yönetin.</p>
      </div>
      <div className="divide-y divide-border">
        <StatusRow
          label="Aktiflik"
          description={collection.isActive ? "Koleksiyon satış yüzeylerinde kullanılabilir." : "Koleksiyon satış yüzeylerinde kullanılamaz."}
          active={collection.isActive}
          activeLabel="Aktif"
          inactiveLabel="Pasif"
          action={setCollectionActivationAction.bind(null, collection.id, !collection.isActive)}
          actionLabel={collection.isActive ? "Pasifleştir" : "Etkinleştir"}
        />
        <StatusRow
          label="Vitrin"
          description={collection.isFeatured ? "Koleksiyon vitrinde öne çıkarılacak şekilde işaretli." : "Koleksiyon standart görünürlükte."}
          active={collection.isFeatured}
          activeLabel="Öne çıkarılmış"
          inactiveLabel="Standart"
          action={setCollectionFeaturedAction.bind(null, collection.id, !collection.isFeatured)}
          actionLabel={collection.isFeatured ? "Öne çıkarmayı kaldır" : "Öne çıkar"}
        />
      </div>
    </section>
  );
}

// Burada her durumun mevcut değerini ve bir sonraki güvenli eylemi aynı satırda gösteriyorum.
function StatusRow({ label, description, active, activeLabel, inactiveLabel, action, actionLabel }: { label: string; description: string; active: boolean; activeLabel: string; inactiveLabel: string; action: () => Promise<void>; actionLabel: string }) {
  return (
    <div className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="text-sm font-semibold text-foreground">{label}</h3>
          <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-semibold ${active ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-slate-200 bg-slate-50 text-slate-700"}`}>{active ? activeLabel : inactiveLabel}</span>
        </div>
        <p className="mt-1 text-xs leading-5 text-muted">{description}</p>
      </div>
      <form action={action}>
        <button type="submit" className="inline-flex min-h-10 w-full items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground outline-none hover:bg-surface-subtle focus-visible:ring-2 focus-visible:ring-primary sm:min-h-9 sm:w-auto">{actionLabel}</button>
      </form>
    </div>
  );
}
