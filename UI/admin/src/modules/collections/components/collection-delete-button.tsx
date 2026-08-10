"use client";

import { EntityDeleteButton } from "@/lib/admin/components/entity-delete-button";
import { deleteCollectionAction } from "@/modules/collections/actions";

// Burada koleksiyon silme sözleşmesini ortak onay düğmesine bağlıyorum.
export function CollectionDeleteButton({ id, name }: { id: string; name: string }) {
  return <EntityDeleteButton entityName={name} title="Koleksiyon silinsin mi?" description={`“${name}” koleksiyonu silinecek. Ürünler korunacak; yalnız koleksiyon bağlantıları kaldırılacak.`} confirmLabel="Koleksiyonu sil" onDelete={() => deleteCollectionAction(id)} />;
}
