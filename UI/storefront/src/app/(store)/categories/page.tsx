import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";

import { siteConfig } from "@/lib/site-config";
import { getCategoryShowcase } from "@/modules/catalog/categories";
import {
  CATEGORIES_DEFAULT_PAGE_SIZE,
  categoriesHref,
  categoriesSearchParamsNeedRedirect,
  parseCategoriesView,
  type CategoriesSearchParams,
} from "@/modules/catalog/categories-query";
import { CategoryShowcase } from "@/modules/catalog/components/category-showcase";

type CategoriesPageProps = {
  searchParams: Promise<CategoriesSearchParams>;
};

// Burada her kategori vitrin sayfasına canonical verirken özel pageSize görünümünü index dışında tutuyorum.
export async function generateMetadata({ searchParams }: CategoriesPageProps): Promise<Metadata> {
  const view = parseCategoriesView(await searchParams);
  const canonical = categoriesHref(view);
  const title = view.page > 1 ? `Kategoriler · Sayfa ${view.page}` : "Kategoriler";
  const description = `${siteConfig.name} kategorilerini ve her kategorideki yayınlanmış ürünleri inceleyin.`;

  return {
    title,
    description,
    alternates: { canonical },
    robots: view.pageSize === CATEGORIES_DEFAULT_PAGE_SIZE
      ? { index: true, follow: true }
      : { index: false, follow: true },
    openGraph: { type: "website", url: canonical, title, description },
  };
}

// Burada URL sayfalamasını doğrulayıp tek server-only kategori isteğiyle vitrini oluşturuyorum.
export default async function CategoriesPage({ searchParams }: CategoriesPageProps) {
  const rawSearchParams = await searchParams;
  const view = parseCategoriesView(rawSearchParams);
  if (categoriesSearchParamsNeedRedirect(rawSearchParams, view)) redirect(categoriesHref(view));

  const categories = await getCategoryShowcase(view.page, view.pageSize);
  if (view.page > 1 && categories.items.length === 0) notFound();

  return <CategoryShowcase page={categories} />;
}
