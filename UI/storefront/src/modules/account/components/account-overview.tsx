import Link from "next/link";

import { formatCurrency } from "@/lib/formatting/currency";
import { AccountIcon } from "@/modules/account/components/account-icon";
import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import { ProfileEditor } from "@/modules/account/components/profile-editor";
import type { AccountAddress, AccountOrderPage, AccountUser } from "@/modules/account/contracts";
import { formatAccountDate, orderStatusLabel } from "@/modules/account/presentation";

// Burada genel bakışı doğrulanmış profil, son sipariş ve varsayılan adres verileriyle sade bir hesap merkezine dönüştürüyorum.
export function AccountOverview({ user, addresses, orders }: { user: AccountUser; addresses: AccountAddress[]; orders: AccountOrderPage }) {
  const defaultShipping = addresses.find((address) => address.type === 0 && address.isDefault);
  const defaultBilling = addresses.find((address) => address.type === 1 && address.isDefault);

  return (
    <section>
      <AccountPageHeader
        eyebrow="Müşteri hesabı"
        title={`Merhaba, ${user.firstName}`}
        description="Kişisel bilgilerinizi, siparişlerinizi ve teslimat tercihlerinizi tek yerden yönetin."
      />

      <div className="mt-7 grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)]">
        <ProfileEditor user={user} />
        <section className="border border-line bg-surface" aria-labelledby="quick-access-title">
          <div className="border-b border-line px-5 py-4"><h2 id="quick-access-title" className="text-base font-black text-ink">Hızlı erişim</h2></div>
          <nav aria-label="Hesap hızlı bağlantıları" className="divide-y divide-line">
            <QuickLink href="/account/orders" label="Siparişlerim" detail={`${orders.totalCount} sipariş`} icon="orders" />
            <QuickLink href="/account/addresses" label="Adreslerim" detail={`${addresses.length} kayıtlı adres`} icon="addresses" />
            <QuickLink href="/account/favorites" label="Favorilerim" detail="Kaydettiğiniz ürünler" icon="favorites" />
            <QuickLink href="/account/security" label="Güvenlik" detail="Parola ve oturumlar" icon="security" />
          </nav>
        </section>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)]">
        <RecentOrders orders={orders} />
        <section className="border border-line bg-surface" aria-labelledby="default-addresses-title">
          <div className="flex items-center justify-between gap-4 border-b border-line px-5 py-4">
            <h2 id="default-addresses-title" className="text-base font-black text-ink">Varsayılan adresler</h2>
            <Link href="/account/addresses" className="focus-ring text-xs font-bold text-brand-700 underline-offset-4 hover:underline">Yönet</Link>
          </div>
          <div className="divide-y divide-line">
            <AddressSummary label="Teslimat" address={defaultShipping} />
            <AddressSummary label="Fatura" address={defaultBilling} />
          </div>
        </section>
      </div>
    </section>
  );
}

// Burada sık kullanılan hesap hedeflerini büyük dekoratif kartlar yerine taranabilir satırlar halinde sunuyorum.
function QuickLink({ href, label, detail, icon }: { href: string; label: string; detail: string; icon: "orders" | "addresses" | "favorites" | "security" }) {
  return (
    <Link href={href} prefetch={false} className="focus-ring group grid min-h-16 grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 px-5 py-3 hover:bg-surface-subtle">
      <AccountIcon icon={icon} className="size-5 text-brand-700" />
      <span><span className="block text-sm font-bold text-ink group-hover:text-brand-700">{label}</span><span className="mt-0.5 block text-xs text-ink-muted">{detail}</span></span>
      <span aria-hidden="true" className="text-brand-700">→</span>
    </Link>
  );
}

// Burada genel bakışta yalnız ilk üç authoritative sipariş özetini gösteriyorum.
function RecentOrders({ orders }: { orders: AccountOrderPage }) {
  return (
    <section className="border border-line bg-surface" aria-labelledby="recent-orders-title">
      <div className="flex items-center justify-between gap-4 border-b border-line px-5 py-4 sm:px-6">
        <h2 id="recent-orders-title" className="text-base font-black text-ink">Son siparişler</h2>
        <Link href="/account/orders" className="focus-ring text-xs font-bold text-brand-700 underline-offset-4 hover:underline">Tümünü gör</Link>
      </div>
      {orders.items.length ? (
        <ul className="divide-y divide-line">
          {orders.items.slice(0, 3).map((order) => (
            <li key={order.id}>
              <Link href={`/account/orders/${order.id}`} className="focus-ring grid gap-2 px-5 py-4 hover:bg-surface-subtle sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:px-6">
                <span><span className="block text-sm font-black text-ink">#{order.orderNumber}</span><span className="mt-1 block text-xs text-ink-muted">{formatAccountDate(order.createdAt)} · {order.itemCount} ürün</span></span>
                <span className="flex items-center justify-between gap-4 sm:block sm:text-right"><span className="text-xs font-bold text-brand-700">{orderStatusLabel(order.status)}</span><span className="block text-sm font-black tabular-nums text-ink sm:mt-1">{formatCurrency(order.grandTotal)}</span></span>
              </Link>
            </li>
          ))}
        </ul>
      ) : (
        <div className="px-5 py-8 text-center sm:px-6">
          <p className="text-sm font-bold text-ink">Henüz siparişiniz yok</p>
          <p className="mt-2 text-xs leading-5 text-ink-muted">İlk siparişiniz burada güvenli biçimde görüntülenecek.</p>
          <Link href="/products" className="focus-ring mt-5 inline-flex min-h-11 items-center border border-brand-700 px-4 text-sm font-bold text-brand-700 hover:bg-surface-subtle">Ürünleri keşfet</Link>
        </div>
      )}
    </section>
  );
}

// Burada teslimat ve fatura varsayılanlarını eksik kayıt durumunu da açıklayarak gösteriyorum.
function AddressSummary({ label, address }: { label: string; address?: AccountAddress }) {
  return (
    <div className="px-5 py-4">
      <p className="text-xs font-bold tracking-[0.08em] text-brand-700 uppercase">{label}</p>
      {address ? (
        <address className="mt-2 text-sm not-italic leading-6 text-ink-muted">
          <span className="block font-bold text-ink">{address.title}</span>
          <span className="block">{address.district} / {address.city}</span>
          <span className="line-clamp-2 block">{address.fullAddress}</span>
        </address>
      ) : <p className="mt-2 text-sm text-ink-muted">Varsayılan adres seçilmedi.</p>}
    </div>
  );
}
