import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ApiError } from "@/lib/api/problem";
import { getAccountOrder, getProductVariants } from "@/modules/account/api";
import { withAccountSession } from "@/modules/account/session";
import { ReturnRequestForm } from "@/modules/returns/components/return-request-form";

export const metadata: Metadata = { title: "İade veya Değişim Talebi" };

// Burada teslim edilmiş sipariş ile aynı ürünlerin canlı varyantlarını form için sunucuda ve cache dışında hazırlıyorum.
export default async function AccountReturnCreatePage({ params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  let order;
  try {
    order = await withAccountSession(`/account/orders/${orderId}/return`, () => getAccountOrder(orderId));
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }
  if (![5, 8, 9].includes(order.status)) notFound();

  const productIds = [...new Set(order.items.map((item) => item.productId))];
  const pages = await Promise.all(productIds.map((productId) => getProductVariants(productId)));
  const variants = Object.fromEntries(productIds.map((productId, index) => [productId, pages[index].items]));

  return <section><Link href={`/account/orders/${order.id}`} className="focus-ring inline-flex min-h-10 items-center text-sm font-bold text-brand-700">← Siparişe dön</Link><header className="mt-3 border-b border-line pb-6"><p className="text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Satış sonrası</p><h1 className="mt-3 text-2xl font-black text-brand-950 sm:text-3xl">İade veya değişim talebi</h1><p className="mt-2 text-sm leading-6 text-ink-muted">#{order.orderNumber} numaralı siparişten işlem yapmak istediğiniz ürün ve adetleri seçin.</p></header><ReturnRequestForm order={order} variants={variants} /></section>;
}
