import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { CollectionForm } from "@/modules/collections/components/collection-form";

export const metadata: Metadata = { title: "Koleksiyon oluştur" };

// Burada otomatik kural sözleşmesi gelene kadar yalnız gerçek manuel koleksiyon oluşturma yüzeyini sunuyorum.
export default async function NewCollectionPage() {
  await requireAdminPageSession("/collections/new");
  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title="Koleksiyon oluştur" description="Ürünleri elle seçerek yönetebileceğiniz bir koleksiyon oluşturun." backHref="/collections" />
      <CollectionForm mode="create" />
    </div>
  );
}
