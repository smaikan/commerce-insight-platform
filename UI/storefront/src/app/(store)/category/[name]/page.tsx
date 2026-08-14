import type { Metadata } from "next";

import {
  buildClassificationMetadata,
  ClassificationCatalogPage,
} from "@/modules/catalog/components/classification-catalog-page";
import type { CatalogSearchParams } from "@/modules/catalog/types";

type CategoryPageProps = {
  params: Promise<{ name: string }>;
  searchParams: Promise<CatalogSearchParams>;
};

// Burada ürün türü adından türetilen kategori URL'si için özgün başlık ve canonical metadata üretiyorum.
export async function generateMetadata({ params, searchParams }: CategoryPageProps): Promise<Metadata> {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return buildClassificationMetadata({ kind: "category", segment: name, searchParams: query });
}

// Burada URL'den çözülen ürün türü ID'sini ortak ürün katalog görünümüne aktarıyorum.
export default async function CategoryPage({ params, searchParams }: CategoryPageProps) {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return ClassificationCatalogPage({ kind: "category", segment: name, searchParams: query });
}
