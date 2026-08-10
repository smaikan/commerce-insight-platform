import Link from "next/link";
import { BrandActivationButton } from "@/modules/brands/components/brand-activation-button";
import { BrandThumbnail } from "@/modules/brands/components/brand-thumbnail";
import { BrandDeleteButton } from "@/modules/brands/components/brand-delete-button";
import type { BrandPage } from "@/modules/brands/types";

// Burada markaları görsel kimlik, açıklama, bağlantı ve aktiflik bilgileriyle hızlı taranabilir biçimde sunuyorum.
export function BrandTable({ page }: { page: BrandPage }) {
  if (page.items.length === 0) {
    return (
      <div className="px-5 py-14 text-center">
        <h2 className="text-base font-semibold text-foreground">Henüz marka bulunmuyor</h2>
        <p className="mt-2 text-sm text-muted">Ürünlerde kullanılacak ilk markayı oluşturarak başlayın.</p>
        <Link href="/brands/new" className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">
          Marka oluştur
        </Link>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[760px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="w-[36%] px-4 py-2.5">Marka</th>
            <th scope="col" className="px-3 py-2.5">Ayrıntı</th>
            <th scope="col" className="px-3 py-2.5">URL</th>
            <th scope="col" className="px-3 py-2.5">Durum</th>
            <th scope="col" className="px-4 py-2.5 text-right">İşlemler</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((brand) => (
            <tr key={brand.id} className="hover:bg-primary-soft/20">
              <td className="px-4 py-2.5">
                <div className="flex items-center gap-3">
                  <BrandThumbnail imageUrl={brand.imageUrl} name={brand.name} />
                  <div className="min-w-0">
                    <Link href={`/brands/${encodeURIComponent(brand.id)}`} className="block truncate font-semibold text-foreground hover:text-primary-hover">
                      {brand.name}
                    </Link>
                    <p className="mt-0.5 truncate font-mono text-[11px] text-muted">{brand.id}</p>
                  </div>
                </div>
              </td>
              <td className="max-w-sm px-3 py-2.5 text-xs leading-5 text-muted">
                <span className="line-clamp-2">{brand.description || "Açıklama yok"}</span>
              </td>
              <td className="px-3 py-2.5"><span className="block max-w-44 truncate text-muted">{brand.url || "—"}</span></td>
              <td className="px-3 py-2.5"><BrandStatus active={brand.isActive} /></td>
              <td className="px-4 py-2.5">
                <div className="flex items-start justify-end gap-2">
                  <Link href={`/brands/${encodeURIComponent(brand.id)}`} className="inline-flex min-h-9 items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground hover:bg-surface-subtle">
                    Düzenle
                  </Link>
                  <BrandActivationButton id={brand.id} isActive={brand.isActive} name={brand.name} />
                  <BrandDeleteButton id={brand.id} name={brand.name} />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Burada marka aktifliğini metin ve renkle birlikte kararlı bir durum rozeti olarak gösteriyorum.
function BrandStatus({ active }: { active: boolean }) {
  return (
    <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-semibold ${active ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-slate-200 bg-slate-50 text-slate-700"}`}>
      {active ? "Aktif" : "Pasif"}
    </span>
  );
}
