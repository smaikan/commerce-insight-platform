import Link from "next/link";
import {
  formatOrderAmount,
  formatOrderDate,
  orderStatusClass,
  orderStatusLabel,
  paymentProviderLabel,
  paymentStatusClass,
  paymentStatusLabel,
} from "@/modules/orders/presentation";
import { OrderReturnManagement } from "@/modules/orders/components/order-return-management";
import { OrderStatusControl } from "@/modules/orders/components/order-status-control";
import { returnStatusClass, returnStatusLabel, returnTypeLabel } from "@/modules/orders/return-presentation";
import type { Order, OrderPayment, ReturnRequest } from "@/modules/orders/types";

// Burada sipariş snapshot'ını kalemler ve ödemeler ana alanda, toplam ve teslimat bağlamı yan rayda olacak biçimde sunuyorum.
export function OrderDetail({ order, returns, returnsUnavailable = false }: { order: Order; returns: ReturnRequest[]; returnsUnavailable?: boolean }) {
  return (
    <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_21rem]">
      <div className="min-w-0 space-y-6">
        <OrderItems order={order} returns={returns} />
        <OrderReturnManagement
          orderId={order.id}
          orderItems={order.items.map((item) => ({ id: item.id, quantity: item.quantity }))}
          returns={returns}
          unavailable={returnsUnavailable}
        />
        <OrderPayments payments={order.payments} />
      </div>

      <aside className="space-y-6 lg:sticky lg:top-24">
        <DetailSection title="Sipariş özeti">
          <div className="flex items-center justify-between gap-3 border-b border-border pb-4">
            <span className="text-sm text-muted">Durum</span>
            <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-bold ${orderStatusClass(order.status)}`}>{orderStatusLabel(order.status)}</span>
          </div>
          <dl className="mt-4 space-y-3 text-sm">
            <SummaryRow label="Ara toplam" value={formatOrderAmount(order.subTotal)} />
            <SummaryRow label="İndirim" value={formatOrderAmount(order.discountTotal)} />
            <SummaryRow label="Kargo" value={formatOrderAmount(order.shippingTotal)} />
            <SummaryRow label="Vergi" value={formatOrderAmount(order.taxTotal)} />
            <div className="flex items-end justify-between gap-4 border-t border-border pt-4">
              <dt className="font-semibold text-foreground">Genel toplam</dt>
              <dd className="text-lg font-bold tabular-nums text-foreground">{formatOrderAmount(order.grandTotal)}</dd>
            </div>
          </dl>
          {order.couponCode ? (
            <div className="mt-4 rounded-lg border border-border bg-surface-subtle px-3 py-2 text-xs text-muted">
              Kupon: <span className="font-mono font-semibold text-foreground">{order.couponCode}</span>
            </div>
          ) : null}
        </DetailSection>

        <DetailSection title="Durum yönetimi">
          <OrderStatusControl key={`${order.id}-${order.status}-${order.trackingNumber ?? ""}`} order={{
            id: order.id,
            orderNumber: order.orderNumber,
            status: order.status,
            shippingCarrier: order.shippingCarrier,
            trackingNumber: order.trackingNumber,
            trackingUrl: order.trackingUrl,
          }} />
        </DetailSection>

        <DetailSection title="Müşteri ve adresler">
          {order.customer ? (
            <dl className="space-y-3 text-sm">
              <InfoRow label="Ad soyad" value={`${order.customer.firstName} ${order.customer.lastName}`} />
              <InfoRow label="E-posta" value={order.customer.email} breakValue />
              <InfoRow label="Telefon" value={order.customer.phoneNumber} />
            </dl>
          ) : <EmptyDetail text="Müşteri snapshot'ı bulunmuyor." />}

          <div className="mt-5 border-t border-border pt-4">
            <h3 className="mb-3 text-sm font-semibold text-foreground">Teslimat adresi</h3>
            {order.shippingAddress ? <AddressBlock address={order.shippingAddress} /> : <EmptyDetail text="Teslimat adresi bulunmuyor." />}
            {order.shippingMethodName ? (
              <p className="mt-3 text-sm text-muted">Kargo yöntemi: <span className="font-semibold text-foreground">{order.shippingMethodName}</span></p>
            ) : null}
            {order.shippingCarrier || order.trackingNumber ? (
              <dl className="mt-3 space-y-2 border-t border-border pt-3 text-sm">
                <InfoRow label="Taşıyıcı" value={order.shippingCarrier || "—"} />
                <InfoRow label="Takip numarası" value={order.trackingNumber || "—"} breakValue />
                {order.trackingUrl ? <InfoRow label="Takip bağlantısı" value={order.trackingUrl} breakValue /> : null}
              </dl>
            ) : null}
          </div>

          <div className="mt-5 border-t border-border pt-4">
            <h3 className="mb-3 text-sm font-semibold text-foreground">Fatura adresi</h3>
            {order.billingAddress ? <AddressBlock address={order.billingAddress} /> : <EmptyDetail text="Fatura adresi snapshot'ı bulunmuyor." />}
          </div>
        </DetailSection>

        <DetailSection title="Zaman bilgileri">
          <dl className="space-y-3 text-sm">
            <InfoRow label="Oluşturuldu" value={formatOrderDate(order.createdAt)} />
            <InfoRow label="Ödendi" value={formatOrderDate(order.paidAt)} />
            <InfoRow label="İptal edildi" value={formatOrderDate(order.cancelledAt)} />
            <InfoRow label="Kargoya verildi" value={formatOrderDate(order.shippedAt)} />
            <InfoRow label="Teslim edildi" value={formatOrderDate(order.deliveredAt)} />
            <InfoRow label="Rezervasyon sonu" value={formatOrderDate(order.reservationExpiresAt)} />
          </dl>
          <p className="mt-3 text-xs leading-5 text-muted">Tarihler Türkiye saatiyle gösterilir.</p>
        </DetailSection>
      </aside>
    </div>
  );
}

// Burada sipariş kalemlerini backend'in fiyat, indirim, vergi ve iade snapshot değerleriyle tablo halinde gösteriyorum.
function OrderItems({ order, returns }: { order: Order; returns: ReturnRequest[] }) {
  return (
    <section aria-labelledby="order-items-title" className="overflow-hidden rounded-xl border border-border bg-surface-strong">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-4 py-4 sm:px-5">
        <div>
          <h2 id="order-items-title" className="text-base font-semibold text-foreground">Sipariş kalemleri</h2>
          <p className="mt-1 text-sm text-muted">{order.items.length} kalem snapshot&apos;ı</p>
        </div>
      </div>

      {order.items.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[880px] border-collapse text-left text-sm">
            <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
              <tr>
                <th scope="col" className="w-[31%] px-5 py-3">Ürün</th>
                <th scope="col" className="px-4 py-3 text-right">Birim fiyat</th>
                <th scope="col" className="px-4 py-3 text-right">Adet</th>
                <th scope="col" className="px-4 py-3 text-right">İndirim</th>
                <th scope="col" className="px-4 py-3 text-right">Vergi</th>
                <th scope="col" className="px-4 py-3 text-right">İade</th>
                <th scope="col" className="px-5 py-3 text-right">Toplam</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/80">
              {order.items.map((item) => {
                const itemReturns = returns.flatMap((returnRequest) => returnRequest.items
                  .filter((returnItem) => returnItem.orderItemId === item.id)
                  .map((returnItem) => ({ returnRequest, returnItem })));
                return (
                  <tr key={item.id} className="align-top hover:bg-primary-soft/20">
                    <td className="px-5 py-4">
                      <p className="font-bold text-foreground">{item.productTitle}</p>
                      <p className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted">
                        <span className="font-mono font-medium text-foreground/75">{item.variantSku}</span>
                        <span aria-hidden="true">•</span>
                        <Link href={`/products/${encodeURIComponent(item.productId)}`} className="font-medium text-primary hover:text-primary-hover">{item.productId}</Link>
                      </p>
                      <p className="mt-1 max-w-72 truncate font-mono text-[11px] text-muted" title={item.productVariantId}>{item.productVariantId}</p>
                      {itemReturns.length > 0 ? (
                        <div className="mt-3 space-y-1.5">
                          {itemReturns.map(({ returnRequest, returnItem }) => (
                            <p key={`${returnRequest.id}-${returnItem.id}`} className="flex flex-wrap items-center gap-1.5 text-xs">
                              <span className={`rounded-md border px-1.5 py-0.5 font-semibold ${returnStatusClass(returnRequest.status)}`}>{returnStatusLabel(returnRequest.status)}</span>
                              <span className="font-semibold text-foreground">{returnItem.quantity} adet {returnTypeLabel(returnRequest.type).toLocaleLowerCase("tr-TR")} talebi</span>
                              <span className="text-muted">· {returnRequest.returnNumber}</span>
                            </p>
                          ))}
                        </div>
                      ) : null}
                    </td>
                    <td className="px-4 py-4 text-right tabular-nums text-foreground">{formatOrderAmount(item.unitPrice)}</td>
                    <td className="px-4 py-4 text-right font-semibold tabular-nums text-foreground">{item.quantity}</td>
                    <td className="px-4 py-4 text-right tabular-nums text-muted">{formatOrderAmount(item.discountTotal)}</td>
                    <td className="px-4 py-4 text-right tabular-nums text-muted">
                      <span className="block">{formatOrderAmount(item.taxTotal)}</span>
                      <span className="mt-1 block text-xs">{item.taxRatePercentage == null ? "Oran yok" : `%${item.taxRatePercentage}`}</span>
                    </td>
                    <td className="px-4 py-4 text-right tabular-nums text-muted">{formatOrderAmount(item.refundTotal)}</td>
                    <td className="px-5 py-4 text-right font-bold tabular-nums text-foreground">{formatOrderAmount(item.totalPrice)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : <div className="px-5 py-10 text-center text-sm text-muted">Bu siparişte kalem snapshot&apos;ı bulunmuyor.</div>}
    </section>
  );
}

// Burada ödeme denemelerini sağlayıcı, durum, tutar ve işlem referansıyla ayrı operasyon yüzeyinde gösteriyorum.
function OrderPayments({ payments }: { payments: OrderPayment[] }) {
  return (
    <section aria-labelledby="order-payments-title" className="overflow-hidden rounded-xl border border-border bg-surface-strong">
      <div className="border-b border-border px-4 py-4 sm:px-5">
        <h2 id="order-payments-title" className="text-base font-semibold text-foreground">Ödemeler</h2>
        <p className="mt-1 text-sm text-muted">Siparişe ait ödeme denemeleri ve sonuçları</p>
      </div>
      {payments.length > 0 ? (
        <div className="divide-y divide-border/80">
          {payments.map((payment) => (
            <article key={payment.id} className="grid gap-4 px-4 py-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:px-5">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="font-semibold text-foreground">{paymentProviderLabel(payment.provider)}</h3>
                  <span className={`inline-flex rounded-md border px-2 py-0.5 text-xs font-bold ${paymentStatusClass(payment.status)}`}>{paymentStatusLabel(payment.status)}</span>
                </div>
                <p className="mt-2 text-xs text-muted">Oluşturuldu: {formatOrderDate(payment.createdAt)}</p>
                <p className="mt-1 text-xs text-muted">Ödendi: {formatOrderDate(payment.paidAt)}</p>
                {payment.transactionId ? <p className="mt-2 break-all font-mono text-xs text-muted">İşlem: {payment.transactionId}</p> : null}
              </div>
              <p className="self-start text-right text-base font-bold tabular-nums text-foreground">{formatOrderAmount(payment.amount)}</p>
            </article>
          ))}
        </div>
      ) : <div className="px-5 py-10 text-center text-sm text-muted">Bu sipariş için ödeme denemesi bulunmuyor.</div>}
    </section>
  );
}

// Burada sipariş bağlamındaki kısa bilgi gruplarını ürün ekranıyla uyumlu yüzeyde topluyorum.
function DetailSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-border bg-surface-strong p-4">
      <h2 className="mb-4 text-base font-semibold text-foreground">{title}</h2>
      {children}
    </section>
  );
}

// Burada toplam satırlarını iki uçta hizalanmış ve tabular sayı kullanan tanım listesi olarak gösteriyorum.
function SummaryRow({ label, value }: { label: string; value: string }) {
  return <div className="flex items-center justify-between gap-4"><dt className="text-muted">{label}</dt><dd className="font-semibold tabular-nums text-foreground">{value}</dd></div>;
}

// Burada müşteri ve zaman bilgilerini uzun değerlerde taşmadan dikey tanım satırı olarak gösteriyorum.
function InfoRow({ label, value, breakValue = false }: { label: string; value: string; breakValue?: boolean }) {
  return <div><dt className="text-xs font-semibold text-muted">{label}</dt><dd className={`mt-1 font-medium text-foreground ${breakValue ? "break-all" : ""}`}>{value}</dd></div>;
}

// Burada teslimat ve fatura snapshot'ını anlamlı satır sırasıyla semantik adres olarak gösteriyorum.
function AddressBlock({ address }: { address: NonNullable<Order["shippingAddress"]> }) {
  return (
    <address className="not-italic text-sm leading-6 text-foreground">
      <p className="font-semibold">{address.firstName} {address.lastName}</p>
      <p className="mt-1 text-muted">{address.title}</p>
      <p className="mt-2 text-muted">{address.fullAddress}</p>
      <p className="text-muted">{address.district} / {address.city}{address.postalCode ? ` · ${address.postalCode}` : ""}</p>
      <p className="mt-2 font-medium">{address.phoneNumber}</p>
    </address>
  );
}

// Burada opsiyonel snapshot bulunmadığında boşluğu kısa ve dürüst bir durumla açıklıyorum.
function EmptyDetail({ text }: { text: string }) {
  return <p className="text-sm leading-6 text-muted">{text}</p>;
}
