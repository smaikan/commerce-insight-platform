import Image from "next/image";
import Link from "next/link";

import type { components } from "@/generated/api";
import { formatCurrency } from "@/lib/formatting/currency";
import { formatVariantLabel } from "@/lib/formatting/variant";
import { orderItemHref } from "@/modules/account/presentation";

type OrderItem = components["schemas"]["OrderItemDto"];

// Burada sipariş snapshot görselini, güvenli ürün bağlantısını ve nullable fallback'leri ek katalog isteği üretmeden sunuyorum.
export function OrderItemMedia({ item }: { item: OrderItem }) {
  const href = orderItemHref(item.productUrl);
  const variantLabel = formatVariantLabel(item.variantName, item.variantValue);
  const image = item.imageUrl ? (
    <Image src={item.imageUrl} alt={item.imageAlt?.trim() || item.productTitle} fill sizes="(max-width: 640px) 72px, 88px" className="object-cover" />
  ) : (
    <span className="flex h-full items-center justify-center px-2 text-center text-[0.625rem] font-semibold leading-4 text-ink-muted">Görsel yok</span>
  );

  return (
    <li className="grid grid-cols-[4.5rem_minmax(0,1fr)] gap-4 px-4 py-4 sm:grid-cols-[5.5rem_minmax(0,1fr)_auto] sm:px-5">
      <div className="relative aspect-[4/5] overflow-hidden border border-line bg-surface-subtle">
        {href ? <Link href={href} aria-label={`${item.productTitle} ürününe git`} className="focus-ring absolute inset-0">{image}</Link> : image}
      </div>
      <div className="min-w-0 self-center">
        {href ? <Link href={href} className="focus-ring font-black text-ink underline-offset-4 hover:text-brand-700 hover:underline">{item.productTitle}</Link> : <p className="font-black text-ink">{item.productTitle}</p>}
        {variantLabel ? <p className="mt-1 break-words text-xs text-ink-muted">{variantLabel}</p> : null}
        <p className="mt-2 text-xs text-ink-muted">{item.quantity} adet · {formatCurrency(item.unitPrice)}</p>
      </div>
      <p className="col-start-2 self-center text-sm font-black tabular-nums text-ink sm:col-start-3 sm:text-right">{formatCurrency(item.totalPrice)}</p>
    </li>
  );
}
