import Link from "next/link";
import { userStatusOptions } from "@/modules/customers/presentation";
import { hasCustomerFilters } from "@/modules/customers/query";
import type { CustomerListQuery } from "@/modules/customers/types";

// Burada müşteri filtre kontrollerinin diğer liste sayfalarıyla aynı yoğunluk ve odak görünümünü kullanıyorum.
const controlClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary sm:min-h-9";

// Burada yalnızca müşteri rolüne sabitlenmiş listede arama ve durum filtrelerini sunuyorum.
export function CustomerFilters({ query }: { query: CustomerListQuery }) {
  return <form action="/customers" method="get" className="border-b border-border bg-surface-subtle/60 p-4 sm:p-5"><div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(14rem,1fr)_minmax(11rem,0.6fr)_minmax(10rem,0.55fr)_auto_auto]"><label><span className="mb-1.5 block text-xs font-semibold text-muted">Ad, soyad veya e-posta</span><input name="search" type="search" defaultValue={query.search ?? ""} placeholder="Ara…" className={controlClass} autoComplete="off" /></label><label><span className="mb-1.5 block text-xs font-semibold text-muted">Durum</span><select name="status" defaultValue={query.status ?? ""} className={controlClass}><option value="">Tüm durumlar</option>{userStatusOptions.map((status) => <option key={status.value} value={status.value}>{status.label}</option>)}</select></label><label><span className="mb-1.5 block text-xs font-semibold text-muted">Sayfa boyutu</span><select name="pageSize" defaultValue={query.pageSize} className={controlClass}>{[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size} kayıt / sayfa</option>)}</select></label><button type="submit" className="min-h-10 cursor-pointer self-end rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover sm:min-h-9">Uygula</button>{hasCustomerFilters(query) ? <Link href="/customers" className="inline-flex min-h-10 cursor-pointer self-end items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-medium text-foreground transition-colors hover:bg-surface-subtle sm:min-h-9">Temizle</Link> : null}</div></form>;
}
