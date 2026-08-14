import type { Metadata } from "next";

import {
  buildClassificationMetadata,
  ClassificationCatalogPage,
} from "@/modules/catalog/components/classification-catalog-page";
import type { CatalogSearchParams } from "@/modules/catalog/types";

type BrandPageProps = {
  params: Promise<{ name: string }>;
  searchParams: Promise<CatalogSearchParams>;
};

// Burada marka başlığını ve canonical URL'yi API'nin yönettiği marka adı/url kaydından üretiyorum.
export async function generateMetadata({ params, searchParams }: BrandPageProps): Promise<Metadata> {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return buildClassificationMetadata({ kind: "brand", segment: name, searchParams: query });
}

// Burada URL'den çözülen marka ID'sini ortak ürün katalog görünümüne aktarıyorum.
export default async function BrandPage({ params, searchParams }: BrandPageProps) {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return ClassificationCatalogPage({ kind: "brand", segment: name, searchParams: query });
}
