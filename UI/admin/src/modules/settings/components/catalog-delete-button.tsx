"use client";

import { EntityDeleteButton } from "@/lib/admin/components/entity-delete-button";
import { deleteCatalogItemAction } from "@/modules/settings/catalog-actions";
import { catalogResourceConfigs, type CatalogResource } from "@/modules/settings/catalog-resource";

// Burada ürün türü ve etiket silme işlemlerini ortak onay davranışına bağlıyorum.
export function CatalogDeleteButton({ resource, id, name }: { resource: CatalogResource; id: string; name: string }) {
  const title = catalogResourceConfigs[resource].singularTitle;
  const description = resource === "product-types"
    ? `“${name}” ürün türü silinecek. Bu türe bağlı ürünler korunacak ve tür ataması kaldırılacak.`
    : `“${name}” etiketi silinecek. Ürünler korunacak ve etiket ürünlerden kaldırılacak.`;
  return <EntityDeleteButton entityName={name} title={`${title} silinsin mi?`} description={description} confirmLabel={`${title} kaydını sil`} onDelete={() => deleteCatalogItemAction(resource, id)} />;
}
