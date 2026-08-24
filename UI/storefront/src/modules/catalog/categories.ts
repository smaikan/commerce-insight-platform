import "server-only";

import { cache } from "react";

import type { components, paths } from "@/generated/api";
import { apiGet } from "@/lib/api/client";
import { classificationSegmentFromName } from "@/modules/catalog/classification-url";
import { catalogHref } from "@/modules/catalog/query";
import { selectMostPopulated } from "@/modules/catalog/showcase-ranking";

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

// Burada endpointin ada göre sayfalamasının tamamını gerektiğinde paralel okuyup ana sayfa için global en yoğun iki kategoriyi seçiyorum.
export const getMostPopulatedCategories = cache(async (limit = 2): Promise<CategoryShowcaseItem[]> => {
  const firstPage = await getCategoryShowcase(1, 100);
  const remainingPages = firstPage.totalPages > 1
    ? await Promise.all(
      Array.from({ length: firstPage.totalPages - 1 }, (_, index) => getCategoryShowcase(index + 2, 100)),
    )
    : [];
  const items = [firstPage, ...remainingPages].flatMap((page) => page.items);

  return selectMostPopulated(items, limit).map((category) => ({
    ...category,
    href: `/category/${encodeURIComponent(classificationSegmentFromName(category.name))}`,
  }));
});
