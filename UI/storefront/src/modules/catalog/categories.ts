import "server-only";

import { cache } from "react";

import type { components, paths } from "@/generated/api";
import { apiGet } from "@/lib/api/client";
import { catalogHref } from "@/modules/catalog/query";

type PublishedCategoryPage = components["schemas"]["PublishedProductTypeShowcaseItemDtoPagedResult"];
type PublishedCategoryQuery = NonNullable<
  paths["/api/product-types/published"]["get"]["parameters"]["query"]
>;

export type CategoryShowcaseItem = components["schemas"]["PublishedProductTypeShowcaseItemDto"] & {
  href: string;
  imageAlt: string;
};

export type CategoryShowcasePage = Omit<PublishedCategoryPage, "items"> & {
  items: CategoryShowcaseItem[];
};

// Burada kategori vitrinini ürün başına ek sorgu üretmeden tek public endpointten alıyorum.
export const getCategoryShowcase = cache(async (
  pageNumber: number,
  pageSize: number,
): Promise<CategoryShowcasePage> => {
  const query: PublishedCategoryQuery = {
    PageNumber: pageNumber,
    PageSize: pageSize,
  };
  const search = new URLSearchParams();
  search.set("PageNumber", String(query.PageNumber));
  search.set("PageSize", String(query.PageSize));

  const page = await apiGet<PublishedCategoryPage>(
    `/api/product-types/published?${search.toString()}`,
    { revalidate: 30, tags: ["published-product-types"] },
  );

  return {
    ...page,
    // Burada adı slug'a çevirmeden kategori kimliğini mevcut TypeId katalog filtresine taşıyorum.
    items: page.items.map((category) => ({
      ...category,
      href: catalogHref({ page: 1, sort: "newest", typeId: category.id }),
      imageAlt: category.name,
    })),
  };
});
