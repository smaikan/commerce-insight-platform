import "server-only";

import { cache } from "react";

import { getCatalogFacets } from "@/modules/catalog/api";
import { catalogSegmentFromApiUrl, classificationSegmentFromName } from "@/modules/catalog/classification-url";
import type { CatalogFilterKey } from "@/modules/catalog/query";
import type { Brand, Collection, ProductType } from "@/modules/catalog/types";

export type CatalogClassificationKind = "brand" | "collection" | "category";

export type CatalogClassification = {
  kind: CatalogClassificationKind;
  id: string;
  name: string;
  description?: string | null;
  segment: string;
  filterKey: CatalogFilterKey;
  eyebrow: string;
  emptyDescription: string;
};

// Burada URL segmentini aktif sınıflandırma kaydıyla çözüp ürün sorgusuna yalnız authoritative ID bilgisini veriyorum.
export const resolveCatalogClassification = cache(async (
  kind: CatalogClassificationKind,
  segment: string,
): Promise<CatalogClassification | null> => {
  const facets = await getCatalogFacets();
  const normalizedSegment = catalogSegmentFromApiUrl(segment);

  if (kind === "brand") {
    const brand = uniqueMatch(facets.brands, (item) => catalogSegmentFromApiUrl(item.url) === normalizedSegment);
    return brand ? fromBrand(brand) : null;
  }

  if (kind === "collection") {
    const collection = uniqueMatch(facets.collections, (item) => catalogSegmentFromApiUrl(item.url) === normalizedSegment);
    return collection ? fromCollection(collection) : null;
  }

  const productType = uniqueMatch(
    facets.productTypes,
    (item) => classificationSegmentFromName(item.name) === normalizedSegment,
  );
  return productType ? fromProductType(productType) : null;
});

function uniqueMatch<T>(items: T[], predicate: (item: T) => boolean): T | null {
  const matches = items.filter(predicate);
  return matches.length === 1 ? matches[0] : null;
}

function fromBrand(brand: Brand): CatalogClassification {
  return {
    kind: "brand",
    id: brand.id,
    name: brand.name,
    description: brand.description,
    segment: catalogSegmentFromApiUrl(brand.url),
    filterKey: "brandId",
    eyebrow: "Marka",
    emptyDescription: "Bu markaya ait yayımlanmış ürün henüz bulunmuyor.",
  };
}

function fromCollection(collection: Collection): CatalogClassification {
  return {
    kind: "collection",
    id: collection.id,
    name: collection.name,
    description: collection.description,
    segment: catalogSegmentFromApiUrl(collection.url),
    filterKey: "collectionId",
    eyebrow: "Koleksiyon",
    emptyDescription: "Bu koleksiyonda yayımlanmış ürün henüz bulunmuyor.",
  };
}

function fromProductType(productType: ProductType): CatalogClassification {
  return {
    kind: "category",
    id: productType.id,
    name: productType.name,
    description: productType.description,
    segment: classificationSegmentFromName(productType.name),
    filterKey: "typeId",
    eyebrow: "Kategori",
    emptyDescription: "Bu kategoride yayımlanmış ürün henüz bulunmuyor.",
  };
}
