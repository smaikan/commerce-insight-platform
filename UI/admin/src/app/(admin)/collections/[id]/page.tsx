import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getCollection } from "@/modules/collections/api";
import { CollectionForm } from "@/modules/collections/components/collection-form";
import { CollectionStatusPanel } from "@/modules/collections/components/collection-status-panel";

export const metadata: Metadata = { title: "Koleksiyon düzenle" };

// Burada koleksiyon detayını yetkili kaynaktan okuyup içerik ve yayın kontrollerini birlikte sunuyorum.
export default async function EditCollectionPage({ params, searchParams }: { params: Promise<{ id: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const { id } = await params;
  const query = await searchParams;
  const session = await requireAdminPageSession(`/collections/${encodeURIComponent(id)}`);
  let collection;
  try {
    collection = await getCollection(id, session);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title="Koleksiyonu düzenle" description="Manuel koleksiyon bilgilerini ve yayın durumunu yönetin." backHref="/collections" />
      <CollectionEditNotice params={query} />
      <CollectionForm collection={collection} mode="edit" />
      <CollectionStatusPanel collection={collection} />
    </div>
  );
}

// Burada düzenleme ve durum işlemlerinin kalıcı sonucunu erişilebilir bir bildirimle gösteriyorum.
function CollectionEditNotice({ params }: { params: Record<string, string | string[] | undefined> }) {
  const error = single(params.error);
  if (error) {
    const message = error === "conflict"
      ? "Bu bağlantı adresi başka bir koleksiyon tarafından kullanılıyor."
      : error === "forbidden"
        ? "Bu işlem için yönetici yetkiniz bulunmuyor."
        : error === "not-found"
          ? "Koleksiyon artık bulunamıyor. Listeyi yenileyin."
          : error === "validation"
            ? "Zorunlu alanları ve karakter sınırlarını kontrol edin."
            : "Koleksiyon işlemi tamamlanamadı. Lütfen tekrar deneyin.";
    return <p role="alert" className="mb-5 rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900">{message}</p>;
  }
  if (params.updated === "1") return <p role="status" className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Koleksiyon bilgileri güncellendi.</p>;
  if (params.created === "1") return <p role="status" className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Koleksiyon oluşturuldu.</p>;
  if (params.status === "activation") return <p role="status" className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Koleksiyon aktiflik durumu güncellendi.</p>;
  if (params.status === "featured") return <p role="status" className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900">Koleksiyon vitrin durumu güncellendi.</p>;
  return null;
}

// Burada tekrarlı URL parametrelerinden ilk bildirim değerini seçiyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
