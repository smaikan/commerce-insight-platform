import Link from "next/link";
import {
  formatUserDate,
  formatUserDateOnly,
  userStatusClass,
  userStatusLabel,
} from "@/modules/customers/presentation";
import { hasCustomerFilters } from "@/modules/customers/query";
import type { AdminUser, CustomerListQuery, UserPage } from "@/modules/customers/types";

// Burada müşteri listesini diğer operasyon tablolarıyla aynı kompakt yoğunlukta ve semantik durum rozetleriyle sunuyorum.
export function CustomerTable({
  page,
  query,
}: {
  page: UserPage;
  query: CustomerListQuery;
}) {
  if (page.items.length === 0) {
    return (
      <div className="px-5 py-14 text-center">
        <h2 className="text-base font-semibold text-foreground">
          {hasCustomerFilters(query)
            ? "Filtrelere uyan müşteri bulunamadı"
            : "Henüz kayıtlı müşteri bulunmuyor"}
        </h2>
        <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-muted">
          {hasCustomerFilters(query)
            ? "Arama veya filtre kriterlerini değiştirerek tekrar deneyin."
            : "Yeni müşteriler kaydoldukça bu listede görünecek."}
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[860px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="w-[30%] px-4 py-2.5">
              Müşteri
            </th>
            <th scope="col" className="px-3 py-2.5">
              İletişim
            </th>
            <th scope="col" className="px-3 py-2.5 text-right">
              Sipariş sayısı
            </th>
            <th scope="col" className="px-3 py-2.5">
              Durum
            </th>
            <th scope="col" className="px-3 py-2.5">
              Son giriş
            </th>
            <th scope="col" className="px-3 py-2.5">
              Kayıt tarihi
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((user) => (
            <CustomerRow key={user.id} user={user} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

// Burada her müşteri satırını baş harf işareti, iletişim, rol, durum ve tarih alanlarıyla hızlı taranabilir biçimde oluşturuyorum.
function CustomerRow({ user }: { user: AdminUser }) {
  const initials = `${user.firstName[0] ?? ""}${user.lastName[0] ?? ""}`.toUpperCase();
  const fullName = `${user.firstName} ${user.lastName}`.trim();

  return (
    <tr className="group bg-surface-strong align-middle transition-colors hover:bg-primary-soft/20">
      {/* Ad Soyad + public ID */}
      <td className="px-4 py-2.5">
        <div className="flex min-w-0 items-center gap-2.5">
          {/* Burada görsel olmayan müşteri için ad baş harflerinden oluşan avatar gösteriyorum. */}
          <span
            aria-hidden="true"
            className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-primary/15 bg-primary-soft/40 text-xs font-bold text-primary"
          >
            {initials}
          </span>
          <span className="min-w-0">
            <Link href={`/customers/${encodeURIComponent(user.id)}`} className="block truncate text-sm font-bold leading-5 text-foreground outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-focus">
              {fullName}
            </Link>
            <span className="mt-0.5 block truncate font-mono text-xs text-muted">
              {user.id}
            </span>
          </span>
        </div>
      </td>

      {/* E-posta + telefon */}
      <td className="px-3 py-2.5">
        <span className="block truncate text-foreground">{user.email}</span>
        {user.phoneNumber ? (
          <span className="mt-0.5 block text-xs text-muted">{user.phoneNumber}</span>
        ) : (
          <span className="mt-0.5 block text-xs text-muted/50">Telefon yok</span>
        )}
      </td>

      {/* Burada API'nin hesapladığı sipariş sayısını müşteri bazında okunabilir olarak gösteriyorum. */}
      <td className="px-3 py-2.5 text-right">
        <span className="font-semibold tabular-nums text-foreground">{user.orderCount ?? 0}</span>
      </td>

      {/* Durum rozeti */}
      <td className="px-3 py-2.5">
        <span
          className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${userStatusClass(user.status)}`}
        >
          {userStatusLabel(user.status)}
        </span>
      </td>

      {/* Son giriş */}
      <td className="whitespace-nowrap px-3 py-2.5 text-sm text-muted">
        {formatUserDate(user.lastLoginAt)}
      </td>

      {/* Kayıt tarihi */}
      <td className="whitespace-nowrap px-3 py-2.5 text-sm text-muted">
        {formatUserDateOnly(user.createdAt)}
      </td>
    </tr>
  );
}
