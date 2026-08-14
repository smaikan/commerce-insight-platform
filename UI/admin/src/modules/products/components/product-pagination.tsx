import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildProductListHref, productSortOptions } from "@/modules/products/query";
import type { PagedResult, Product, ProductListQuery } from "@/modules/products/types";

// Burada ürün filtrelerini ve sıralamasını koruyarak ortak admin sayfalama düzenini kullanıyorum.
export function ProductPagination({ page, query }: { page: PagedResult<Product>; query: ProductListQuery }) {
  const sort = productSortOptions.find(
    (option) => option.sortBy === query.sortBy && option.descending === query.descending,
  )?.value ?? productSortOptions[0].value;
  const hiddenFields = [
    { name: "pageSize", value: query.pageSize },
    ...(query.search ? [{ name: "search", value: query.search }] : []),
    ...(query.typeId ? [{ name: "typeId", value: query.typeId }] : []),
    ...(query.brandId ? [{ name: "brandId", value: query.brandId }] : []),
    ...(query.status !== undefined ? [{ name: "status", value: query.status }] : []),
    ...(query.isFeatured !== undefined ? [{ name: "isFeatured", value: query.isFeatured }] : []),
    { name: "sort", value: sort },
  ];

  return (
    <AdminPagination
      action="/products"
      ariaLabel="Ürün listesi sayfalama"
      buildHref={(pageNumber) => buildProductListHref(query, pageNumber)}
      hiddenFields={hiddenFields}
      itemLabel="ürün"
      pageNumber={page.pageNumber}
      pageParam="page"
      pageSize={page.pageSize}
      totalCount={page.totalCount}
      totalPages={page.totalPages}
    />
  );
}
