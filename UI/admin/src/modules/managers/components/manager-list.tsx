import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { managerHref } from "@/modules/managers/query";
import type { Manager, ManagerPage, ManagerQuery } from "@/modules/managers/types";

const dateFormatter = new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeZone: "Europe/Istanbul" });

// Burada yönetici kayıtlarını hesap güvenliği için anlamlı durum ve oturum bilgileriyle listeliyorum.
export function ManagerList({ page, query }: { page: ManagerPage; query: ManagerQuery }) {
  return (
    <section aria-label="Yönetici listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
      <ManagerFilters query={query} />
      {page.items.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] border-collapse text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/60 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
              <tr>
                <th scope="col" className="w-[32%] px-4 py-2.5 sm:px-5">Yönetici</th>
                <th scope="col" className="px-3 py-2.5">Telefon</th>
                <th scope="col" className="px-3 py-2.5">Durum</th>
                <th scope="col" className="px-3 py-2.5">Son giriş</th>
                <th scope="col" className="px-4 py-2.5 sm:px-5">Kayıt tarihi</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/80">
              {page.items.map((manager) => <ManagerRow key={manager.id} manager={manager} />)}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="px-5 py-12 text-center">
          <h2 className="text-base font-semibold text-foreground">Bu aramayla eşleşen yönetici bulunamadı</h2>
          <p className="mt-1 text-sm text-muted">Arama ifadesini değiştirerek tekrar deneyin.</p>
        </div>
      )}
      <ManagerPagination page={page} query={query} />
    </section>
  );
}

// Burada yalnız belgelenmiş ad/e-posta araması ve sayfa boyutu seçeneklerini URL durumunda tutuyorum.
function ManagerFilters({ query }: { query: ManagerQuery }) {
  return (
    <form action="/managers" method="get" className="grid gap-2 border-b border-border bg-surface-subtle/40 p-3 sm:grid-cols-[minmax(0,1fr)_11rem_auto]">
      <label className="sr-only" htmlFor="manager-search">Yönetici ara</label>
      <input id="manager-search" name="search" type="search" defaultValue={query.search} placeholder="Ad veya e-posta ara" className="min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9" />
      <label className="flex items-center gap-2 text-xs font-semibold text-muted">
        Sayfa boyutu
        <select name="pageSize" defaultValue={query.pageSize} className="min-h-10 flex-1 rounded-lg border border-border-strong bg-surface-strong px-2 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 sm:min-h-9">
          {[20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}
        </select>
      </label>
      <button type="submit" className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus sm:min-h-9">Ara</button>
    </form>
  );
}

// Burada her yönetici satırını desteklenen hesap durumu ve güvenlik açısından anlamlı tarihlerle sunuyorum.
function ManagerRow({ manager }: { manager: Manager }) {
  const status = managerStatusPresentation(manager.status);

  return (
    <tr className="bg-surface-strong align-middle hover:bg-primary-soft/20">
      <td className="px-4 py-3 sm:px-5"><p className="font-semibold text-foreground">{manager.firstName} {manager.lastName}</p><p className="mt-1 text-xs text-muted">{manager.email}</p></td>
      <td className="px-3 py-3 text-muted">{manager.phoneNumber || "—"}</td>
      <td className="px-3 py-3"><span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${status.className}`}>{status.label}</span></td>
      <td className="px-3 py-3 text-muted">{manager.lastLoginAt ? dateFormatter.format(new Date(manager.lastLoginAt)) : "Henüz giriş yapmadı"}</td>
      <td className="px-4 py-3 text-muted sm:px-5">{dateFormatter.format(new Date(manager.createdAt))}</td>
    </tr>
  );
}

// Burada listelenen yönetici sonuçlarını ortak admin sayfalama düzenine bağlayıp arama durumunu koruyorum.
function ManagerPagination({ page, query }: { page: ManagerPage; query: ManagerQuery }) {
  return (
    <AdminPagination
      action="/managers"
      ariaLabel="Yönetici listesi sayfalama"
      buildHref={(pageNumber) => managerHref(query, pageNumber)}
      hiddenFields={[
        ...(query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []),
        ...(query.search ? [{ name: "search", value: query.search }] : []),
      ]}
      itemLabel="yönetici"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}

// Burada backend kullanıcı durumunu yalnız gerçek semantik renk ve metinle sunuyorum.
function managerStatusPresentation(status: Manager["status"]): { label: string; className: string } {
  if (status === 1) return { label: "Aktif", className: "border-emerald-200 bg-emerald-50 text-emerald-800" };
  if (status === 2) return { label: "Pasif", className: "border-amber-200 bg-amber-50 text-amber-800" };
  if (status === 3) return { label: "Silindi", className: "border-red-200 bg-red-50 text-red-800" };
  return { label: "Bilinmiyor", className: "border-slate-300 bg-slate-100 text-slate-700" };
}
