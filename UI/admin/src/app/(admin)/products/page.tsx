import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getProductListOptions, getProducts } from "@/modules/products/api";
import { ProductFilters } from "@/modules/products/components/product-filters";
import { ProductPagination } from "@/modules/products/components/product-pagination";
import { ProductTable } from "@/modules/products/components/product-table";
import { buildProductListHref, parseProductListQuery } from "@/modules/products/query";

export const metadata: Metadata = { title: "Ürünler" };

// Burada URL filtrelerini okuyup ürün verisiyle seçenekleri sunucuda paralel hazırlıyorum.
export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const params = await searchParams;
  const query = parseProductListQuery(params);
  // Burada sayfa, filtre ve sıralama durumunu auth yenilemesinden sonra aynı liste görünümüne dönmek için koruyorum.
  const session = await requireAdminPageSession(buildProductListHref(query, query.pageNumber));
  const [page, options] = await Promise.all([
    getProducts(query, session),
    getProductListOptions(session),
  ]);
  return (
    <div className="w-full">
      <PageHeader
        title="Ürünler"
        description="Katalog ürünlerini filtreleyin, sıralayın ve yönetin."
        actions={
          <Link href="/products/new" className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">
            Ürün ekle
          </Link>
        }
      />

      {params.deleted === "1" ? <p className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-900" role="status">Ürün arşive taşındı ve mağazadan kaldırıldı.</p> : null}

      <section aria-label="Ürün listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
        <ProductFilters query={query} productTypes={options.productTypes} brands={options.brands} />
        <ProductTable page={page} query={query} />
        <ProductPagination page={page} query={query} />
      </section>
    </div>
  );
}
