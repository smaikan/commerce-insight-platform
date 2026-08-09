import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCoupons } from "@/modules/coupons/api";
import { CouponList } from "@/modules/coupons/components/coupon-list";
import { parseCouponListQuery } from "@/modules/coupons/query";

export const metadata: Metadata = { title: "İndirimler" };

// Burada kupon listesini doğrulanmış yönetici oturumu ve URL filtreleriyle getiriyorum.
export default async function CouponsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [session, params] = await Promise.all([requireAdminPageSession("/coupons"), searchParams]);
  const query = parseCouponListQuery(params);
  const page = await getCoupons(query, session);
  const created = params.created === "1";
  const updated = params.updated === "1";
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="İndirimler" description="Yüzde veya sabit tutarlı kuponları, kullanım koşullarını ve aktiflik durumlarını yönetin." actions={<Link href="/coupons/new" className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Kupon oluştur</Link>} />{created ? <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">Kupon oluşturuldu.</p> : null}{updated ? <p role="status" className="mb-4 rounded-xl border border-success/25 bg-success/10 px-4 py-3 text-sm font-semibold text-success">Kupon güncellendi.</p> : null}<CouponList page={page} query={query} /></div>;
}
