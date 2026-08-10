import Link from "next/link";
import type { CatalogResource } from "@/modules/settings/catalog-resource";
import type { CatalogItem, CatalogPage } from "@/modules/settings/catalog-types";
import { CatalogActivationButton } from "@/modules/settings/components/catalog-activation-button";
import { CatalogDeleteButton } from "@/modules/settings/components/catalog-delete-button";
import { SettingsPagination } from "@/modules/settings/components/settings-pagination";
import { SettingsStatusBadge } from "@/modules/settings/components/status-badge";
import type { SettingsListQuery } from "@/modules/settings/types";

// Burada marka, ürün türü ve etiketleri ortak operasyon yoğunluğunda tek liste bileşeniyle gösteriyorum.
export function CatalogList({ resource, page, query }: { resource: CatalogResource; page: CatalogPage; query: SettingsListQuery }) {
  const basePath = `/settings/catalog/${resource}`;
  return (
    <section className="overflow-hidden rounded-xl border border-border bg-surface" aria-labelledby="catalog-list-title">
      <div className="flex items-center justify-between gap-3 border-b border-border bg-surface-subtle/60 px-4 py-3 sm:px-5"><div><h2 id="catalog-list-title" className="text-base font-semibold text-foreground">Tanımlı kayıtlar</h2><p className="mt-0.5 text-xs text-muted">Aktif kayıtlar ürün oluşturma ve düzenleme seçeneklerinde kullanılabilir.</p></div><span className="text-xs font-semibold text-muted">{page.totalCount} kayıt</span></div>
      {page.items.length ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/40 text-xs font-semibold uppercase tracking-wide text-muted"><tr><th className="px-5 py-3">Ad</th><th className="px-3 py-3">Ayrıntı</th><th className="px-3 py-3">Durum</th><th className="px-5 py-3 text-right">İşlemler</th></tr></thead>
            <tbody className="divide-y divide-border">
              {page.items.map((item) => <CatalogRow key={item.id} resource={resource} item={item} />)}
            </tbody>
          </table>
        </div>
      ) : <div className="px-5 py-12 text-center"><p className="font-semibold text-foreground">Henüz kayıt yok</p><p className="mt-1 text-sm text-muted">Ürünlerde kullanılacak ilk katalog tanımını oluşturun.</p><Link href={`${basePath}/new`} className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">Yeni kayıt ekle</Link></div>}
      <SettingsPagination basePath={basePath} query={query} totalCount={page.totalCount} totalPages={page.totalPages} />
    </section>
  );
}

// Burada her katalog kaydının desteklediği URL veya açıklama bilgisini kaynak tipine göre gösteriyorum.
function CatalogRow({ resource, item }: { resource: CatalogResource; item: CatalogItem }) {
  const detail = catalogItemDetail(item);
  return (
    <tr className="hover:bg-surface-subtle/35">
      <td className="px-5 py-3"><Link href={`/settings/catalog/${resource}/${item.id}`} className="font-semibold text-foreground hover:text-primary-hover">{item.name}</Link><p className="mt-0.5 font-mono text-[11px] text-muted">{item.id}</p></td>
      <td className="max-w-md px-3 py-3 text-xs leading-5 text-muted">{detail}</td>
      <td className="px-3 py-3"><SettingsStatusBadge active={item.isActive} /></td>
      <td className="px-5 py-3"><div className="flex items-start justify-end gap-2"><Link href={`/settings/catalog/${resource}/${item.id}`} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle">Düzenle</Link><CatalogActivationButton resource={resource} id={item.id} isActive={item.isActive} name={item.name} /><CatalogDeleteButton resource={resource} id={item.id} name={item.name} /></div></td>
    </tr>
  );
}

// Burada union DTO'dan yalnızca gerçekten mevcut ikincil alanı güvenli biçimde okuyorum.
function catalogItemDetail(item: CatalogItem): string {
  if ("description" in item && item.description) return item.description;
  if ("url" in item && item.url) return item.url;
  return "Ek bilgi yok";
}
