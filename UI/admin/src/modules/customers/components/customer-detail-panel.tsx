import { formatUserDate, formatUserDateOnly, userRoleClass, userRoleLabel, userStatusClass, userStatusLabel } from "@/modules/customers/presentation";
import { updateCustomerRoleAction, updateCustomerStatusAction } from "@/modules/customers/actions";
import type { CustomerDetail } from "@/modules/customers/types";
import type { CustomerOrderPage } from "@/modules/customers/types";

// Burada belgelenmiş müşteri detay alanlarını operasyon ekranına uygun, kısa bilgi gruplarında sunuyorum.
export function CustomerDetailPanel({ customer, orders }: { customer: CustomerDetail; orders: CustomerOrderPage }) {
  return (
    <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]">
      <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5">
        <h2 className="text-base font-semibold text-foreground">İletişim bilgileri</h2>
        <dl className="mt-4 grid gap-4 sm:grid-cols-2">
          <DetailRow label="E-posta" value={customer.email} breakValue />
          <DetailRow label="Telefon" value={customer.phoneNumber || "—"} />
          <DetailRow label="Müşteri numarası" value={customer.id} mono />
          <DetailRow label="Kayıt tarihi" value={formatUserDateOnly(customer.createdAt)} />
          <DetailRow label="Son giriş" value={formatUserDate(customer.lastLoginAt)} />
          <DetailRow label="Son güncelleme" value={formatUserDate(customer.updatedAt)} />
        </dl>
        <CustomerOrders orders={orders} />
      </section>

      <aside className="space-y-5 lg:sticky lg:top-20">
        <section className="rounded-xl border border-border bg-surface-strong p-4">
          <h2 className="text-base font-semibold text-foreground">Hesap durumu</h2>
          <div className="mt-4 space-y-3 text-sm">
            <p className="flex items-center justify-between gap-3"><span className="text-muted">Rol</span><span className={`rounded-md border px-2 py-0.5 text-xs font-semibold ${userRoleClass(customer.role)}`}>{userRoleLabel(customer.role)}</span></p>
            <p className="flex items-center justify-between gap-3"><span className="text-muted">Durum</span><span className={`rounded-md border px-2 py-0.5 text-xs font-semibold ${userStatusClass(customer.status)}`}>{userStatusLabel(customer.status)}</span></p>
          </div>
          <form action={updateCustomerRoleAction} className="mt-4 border-t border-border pt-4"><input type="hidden" name="customerId" value={customer.id} /><label className="block text-xs font-semibold text-muted">Rol<select name="role" defaultValue={customer.role} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground"><option value={1}>Müşteri</option><option value={2}>Yönetici</option></select></label><button type="submit" className="mt-2 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle">Rolü güncelle</button></form>
          <form action={updateCustomerStatusAction} className="mt-4"><input type="hidden" name="customerId" value={customer.id} /><label className="block text-xs font-semibold text-muted">Hesap durumu<select name="status" defaultValue={customer.status} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground"><option value={1}>Aktif</option><option value={2}>Pasif</option><option value={3}>Silindi</option></select></label><button type="submit" className="mt-2 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle">Durumu güncelle</button></form>
        </section>
      </aside>
    </div>
  );
}

// Burada müşterinin son siparişlerini sipariş numarası, durum ve tarih bilgisiyle gösteriyorum.
function CustomerOrders({ orders }: { orders: CustomerOrderPage }) {
  return <section className="mt-6 border-t border-border pt-5"><div className="flex items-center justify-between gap-3"><h2 className="text-base font-semibold text-foreground">Siparişler</h2><span className="text-sm text-muted">{orders.totalCount} sipariş</span></div>{orders.items.length ? <div className="mt-3 divide-y divide-border rounded-lg border border-border">{orders.items.map((order) => <a key={order.id} href={`/orders/${encodeURIComponent(order.id)}`} className="flex items-center justify-between gap-4 px-3 py-3 hover:bg-surface-subtle"><span><span className="block font-semibold text-foreground">{order.orderNumber}</span><span className="mt-1 block text-xs text-muted">{formatUserDate(order.createdAt)}{order.paidAt ? ` · Ödeme: ${formatUserDate(order.paidAt)}` : ""}</span></span><span className="text-sm font-semibold tabular-nums text-foreground">{new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(order.grandTotal)}</span></a>)}</div> : <p className="mt-3 text-sm text-muted">Bu müşterinin henüz siparişi yok.</p>}</section>;
}

// Burada uzun müşteri değerlerini taşmadan, etiket-değer ilişkisiyle gösteriyorum.
function DetailRow({ label, value, mono = false, breakValue = false }: { label: string; value: string; mono?: boolean; breakValue?: boolean }) {
  return <div><dt className="text-xs font-semibold text-muted">{label}</dt><dd className={`mt-1 text-sm font-medium text-foreground ${mono ? "font-mono" : ""} ${breakValue ? "break-all" : ""}`}>{value}</dd></div>;
}
