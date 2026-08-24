"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { setCouponActivationAction } from "@/modules/coupons/actions";
import { CouponForm } from "@/modules/coupons/components/coupon-form";
import { buildCouponListHref } from "@/modules/coupons/query";
import type { Coupon, CouponListQuery, CouponPage } from "@/modules/coupons/types";

// Burada kupon listesini, satır düzenlemesini ve dar aktiflik işlemini birlikte yönetiyorum.
export function CouponList({ page, query }: { page: CouponPage; query: CouponListQuery }) {
  const [editingCoupon, setEditingCoupon] = useState<Coupon | null>(null);
  const [activationMessage, setActivationMessage] = useState<string>();
  const [isPending, startTransition] = useTransition();

  const changeActivation = (coupon: Coupon) => startTransition(async () => {
    const result = await setCouponActivationAction(coupon.id, !coupon.isActive);
    setActivationMessage(result.status === "error" ? result.message : undefined);
  });

  if (editingCoupon) return <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_18rem]"><div><CouponForm coupon={editingCoupon} onCancel={() => setEditingCoupon(null)} /></div><CouponSummary coupon={editingCoupon} /></div>;

  return <section aria-labelledby="coupon-list-title" className="overflow-hidden rounded-xl border border-border bg-surface"><div className="border-b border-border bg-surface-subtle px-4 py-3 sm:px-5"><div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"><div><h2 id="coupon-list-title" className="text-base font-semibold text-foreground">Kuponlar</h2><p className="mt-1 text-sm text-muted">Her kullanım checkout sırasında backend tarafından doğrulanır ve hesaplanır.</p></div><span className="text-sm font-medium text-muted">{page.totalCount} kupon</span></div></div><CouponFilters query={query} />{activationMessage ? <p role="alert" className="mx-4 mt-3 rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger sm:mx-5">{activationMessage}</p> : null}<div className="overflow-x-auto"><table className="w-full min-w-[940px] border-collapse text-left text-sm"><thead className="border-y border-border bg-surface-subtle/60 text-[11px] font-bold uppercase tracking-[0.08em] text-muted"><tr><th className="px-4 py-2.5 sm:px-5">Kupon</th><th className="px-3 py-2.5">İndirim</th><th className="px-3 py-2.5">Koşullar</th><th className="px-3 py-2.5">Kullanım</th><th className="px-3 py-2.5">Süre</th><th className="px-4 py-2.5 text-right sm:px-5">İşlem</th></tr></thead><tbody className="divide-y divide-border/80">{page.items.map((coupon) => <tr key={coupon.id} className="align-top hover:bg-surface-subtle/45"><td className="px-4 py-3 sm:px-5"><div className="flex items-center gap-2"><code className="rounded-md bg-surface-subtle px-2 py-1 text-xs font-bold tracking-wide text-foreground">{coupon.code}</code><StatusBadge active={coupon.isActive} /></div>{coupon.description ? <p className="mt-1.5 max-w-56 text-xs leading-5 text-muted">{coupon.description}</p> : <p className="mt-1.5 text-xs text-muted">Açıklama yok</p>}</td><td className="px-3 py-3"><p className="font-semibold text-foreground">{formatDiscount(coupon)}</p><p className="mt-1 text-xs text-muted">{coupon.discountType === 0 ? "Yüzde indirimi" : "Sabit tutar indirimi"}</p></td><td className="px-3 py-3"><p className="text-xs leading-5 text-muted">{coupon.minimumOrderAmount == null ? "Minimum sepet koşulu yok" : `Minimum ${formatMoney(coupon.minimumOrderAmount)}`}</p><p className="text-xs leading-5 text-muted">{coupon.isMemberOnly ? "Yalnız üyeler" : "Tüm müşteriler"}</p></td><td className="px-3 py-3"><p className="font-medium tabular-nums text-foreground">{coupon.usedCount} / {coupon.usageLimit ?? "∞"}</p><p className="mt-1 text-xs text-muted">Toplam kullanım</p></td><td className="px-3 py-3"><p className="text-xs leading-5 text-muted">{coupon.startsAt ? `${formatDate(coupon.startsAt)} itibarıyla` : "Hemen başlar"}</p><p className="text-xs leading-5 text-muted">{coupon.expiresAt ? `${formatDate(coupon.expiresAt)} son gün` : "Bitiş tarihi yok"}</p></td><td className="px-4 py-3 sm:px-5"><div className="flex justify-end gap-2"><button type="button" onClick={() => setEditingCoupon(coupon)} className="inline-flex min-h-9 cursor-pointer items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle">Düzenle</button><button type="button" onClick={() => changeActivation(coupon)} disabled={isPending} className="inline-flex min-h-9 cursor-pointer items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">{coupon.isActive ? "Pasifleştir" : "Etkinleştir"}</button></div></td></tr>)}</tbody></table></div>{page.items.length === 0 ? <div className="px-5 py-12 text-center"><p className="font-semibold text-foreground">Bu görünümde kupon yok</p><p className="mt-1 text-sm text-muted">Filtreyi değiştirin veya yeni bir kupon oluşturun.</p></div> : null}<CouponPagination page={page} query={query} /></section>;
}


// Burada kupon filtrelerini koruyarak ortak admin sayfalama ve doğrudan sayfa atlama düzenini kullanıyorum.
function CouponPagination({ page, query }: { page: CouponPage; query: CouponListQuery }) {
  return (
    <AdminPagination
      action="/coupons"
      ariaLabel="Kupon listesi sayfalama"
      buildHref={(pageNumber) => buildCouponListHref(query, pageNumber)}
      hiddenFields={[
        ...(query.pageSize !== 20 ? [{ name: "pageSize", value: query.pageSize }] : []),
        ...(query.isActive !== undefined ? [{ name: "isActive", value: query.isActive }] : []),
      ]}
      itemLabel="kupon"
      pageNumber={page.pageNumber}
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}

// Burada sözleşmedeki tek liste filtresini URL durumunda saklıyorum.
function CouponFilters({ query }: { query: CouponListQuery }) {
  return <form action="/coupons" method="get" className="flex flex-col gap-2 border-b border-border bg-surface-subtle/35 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5"><div className="flex flex-wrap items-center gap-2"><label htmlFor="coupon-active-filter" className="text-sm font-medium text-foreground">Durum</label><select id="coupon-active-filter" name="isActive" defaultValue={query.isActive === undefined ? "" : String(query.isActive)} className="min-h-9 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary"><option value="">Tümü</option><option value="true">Aktif</option><option value="false">Pasif</option></select><button type="submit" className="inline-flex min-h-9 cursor-pointer items-center rounded-lg bg-primary px-3 text-sm font-semibold text-white transition-colors hover:bg-primary-hover">Uygula</button>{query.isActive !== undefined ? <Link href="/coupons" className="inline-flex min-h-9 cursor-pointer items-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle">Temizle</Link> : null}</div><label className="flex items-center gap-2 text-xs font-medium text-muted">Sayfa başına<select name="pageSize" defaultValue={query.pageSize} className="min-h-8 rounded-md border border-border-strong bg-surface-strong px-2 text-xs text-foreground outline-none focus:border-primary">{[20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}</select></label></form>;
}

// Burada düzenleme görünümünde kuponun API kaynaklı özetini sabit bir yan panelde tutuyorum.
function CouponSummary({ coupon }: { coupon: Coupon }) {
  return <aside className="h-fit rounded-xl border border-border bg-surface p-4"><p className="text-xs font-bold uppercase tracking-[0.08em] text-muted">Mevcut kullanım</p><p className="mt-2 font-mono text-xl font-bold text-foreground">{coupon.code}</p><div className="mt-4 space-y-3 text-sm"><SummaryRow label="Kullanım" value={`${coupon.usedCount} / ${coupon.usageLimit ?? "∞"}`} /><SummaryRow label="Oluşturulma" value={formatDate(coupon.createdAt)} /><SummaryRow label="Son güncelleme" value={coupon.updatedAt ? formatDate(coupon.updatedAt) : "—"} /></div><p className="mt-4 rounded-lg bg-surface-subtle px-3 py-2 text-xs leading-5 text-muted">Kullanım geçmişi bu sözleşmede listelenmiyor; sayı backend’in güncel kaydıdır.</p></aside>;
}

function SummaryRow({ label, value }: { label: string; value: string }) { return <div className="flex items-center justify-between gap-3"><span className="text-muted">{label}</span><span className="text-right font-medium text-foreground">{value}</span></div>; }
function StatusBadge({ active }: { active: boolean }) { return <span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${active ? "bg-success/12 text-success" : "bg-surface-subtle text-muted"}`}>{active ? "Aktif" : "Pasif"}</span>; }
function formatDiscount(coupon: Coupon): string { return coupon.discountType === 0 ? `%${coupon.discountValue}` : formatMoney(coupon.discountValue); }
function formatMoney(value: number): string { return new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY", maximumFractionDigits: 2 }).format(value); }
function formatDate(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(date); }
