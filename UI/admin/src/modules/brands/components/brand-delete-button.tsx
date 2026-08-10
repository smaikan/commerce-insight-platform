"use client";

import { EntityDeleteButton } from "@/lib/admin/components/entity-delete-button";
import { deleteBrandAction } from "@/modules/brands/actions";

// Burada marka silme sözleşmesini ortak onay düğmesine bağlıyorum.
export function BrandDeleteButton({ id, name }: { id: string; name: string }) {
  return <EntityDeleteButton entityName={name} title="Marka silinsin mi?" description={`“${name}” markası silinecek. Bu markaya bağlı ürünler korunacak ve markasız olarak devam edecek.`} confirmLabel="Markayı sil" onDelete={() => deleteBrandAction(id)} />;
}
