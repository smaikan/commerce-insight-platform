import type { Metadata } from "next";
import Link from "next/link";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCollections } from "@/modules/collections/api";
import { CollectionPagination } from "@/modules/collections/components/collection-pagination";
import { CollectionTable } from "@/modules/collections/components/collection-table";
import { buildCollectionListHref, parseCollectionListQuery } from "@/modules/collections/query";

export const metadata: Metadata = { title: "Koleksiyonlar" };

// Burada belgelenen sayfalama parametrelerini ve yönetici oturumunu kullanarak koleksiyon yönetimini kuruyorum.
export default async function CollectionsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  const query = parseCollectionListQuery(params);
  const session = await requireAdminPageSession(buildCollectionListHref(query));
  const page = await getCollections(query.pageNumber, query.pageSize, session);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Koleksiyonlar"
        description="Ürünleri müşteriye dönük gruplar halinde düzenleyin; görünürlük ve vitrin durumlarını yönetin."
        actions={<Link href="/collections/new" className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover">Koleksiyon oluştur</Link>}
      />
      <CollectionNotice params={params} />
      <section aria-label="Koleksiyon listesi" className="overflow-hidden rounded-xl border border-border bg-surface">
        <CollectionTable page={page} />
        <CollectionPagination page={page} query={query} />
      </section>
    </div>
  );
}

// Burada kalıcı yönlendirme sonucunu sayfanın üstünde kısa ve erişilebilir bir bildirimle gösteriyorum.
function CollectionNotice({ params }: { params: Record<string, string | string[] | undefined> }) {
  if (params.created !== "1") return null;
  return <p className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900" role="status">Koleksiyon oluşturuldu.</p>;
}
