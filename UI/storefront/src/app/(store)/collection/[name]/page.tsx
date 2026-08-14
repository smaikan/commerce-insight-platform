import type { Metadata } from "next";

import {
  buildClassificationMetadata,
  ClassificationCatalogPage,
} from "@/modules/catalog/components/classification-catalog-page";
import type { CatalogSearchParams } from "@/modules/catalog/types";

type CollectionPageProps = {
  params: Promise<{ name: string }>;
  searchParams: Promise<CatalogSearchParams>;
};

// Burada koleksiyon başlığını ve canonical URL'yi API'nin yönettiği koleksiyon adı/url kaydından üretiyorum.
export async function generateMetadata({ params, searchParams }: CollectionPageProps): Promise<Metadata> {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return buildClassificationMetadata({ kind: "collection", segment: name, searchParams: query });
}

// Burada URL'den çözülen koleksiyon ID'sini ortak ürün katalog görünümüne aktarıyorum.
export default async function CollectionPage({ params, searchParams }: CollectionPageProps) {
  const [{ name }, query] = await Promise.all([params, searchParams]);
  return ClassificationCatalogPage({ kind: "collection", segment: name, searchParams: query });
}
