import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getBrands } from "@/modules/brands/api";
import { BrandPagination } from "@/modules/brands/components/brand-pagination";
import { BrandTable } from "@/modules/brands/components/brand-table";
import { buildBrandListHref, parseBrandListQuery } from "@/modules/brands/query";

export const metadata: Metadata = { title: "Markalar" };

// Burada marka listesini belgelenen sayfalama ve doğrulanmış yönetici oturumuyla sunuyorum.
export default async function BrandsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  const query = parseBrandListQuery(params);
  const session = await requireAdminPageSession(buildBrandListHref(query));
  const page = await getBrands(query.pageNumber, query.pageSize, session);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Markalar"
        description="Ürünlerde kullanılan marka kimliklerini, görsellerini ve kullanılabilirlik durumlarını yönetin."
        actions={<Link href="/brands/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Marka oluştur</Link>}
      />
      <BrandNotice params={params} />
      <section aria-label="Marka listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
        <BrandTable page={page} />
        <BrandPagination page={page} query={query} />
      </section>
    </div>
  );
}

// Burada kalıcı create ve update sonuçlarını liste üzerinde erişilebilir biçimde duyuruyorum.
function BrandNotice({ params }: { params: Record<string, string | string[] | undefined> }) {
  if (params.deleted === "1") return <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Marka silindi.</p>;
  if (params.created === "1") return <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Marka oluşturuldu.</p>;
  if (params.updated === "1") return <p role="status" className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Marka güncellendi.</p>;
  return null;
}
