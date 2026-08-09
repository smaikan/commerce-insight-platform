import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { createManualCollectionAction } from "@/modules/collections/actions";
import { CollectionForm } from "@/modules/collections/components/collection-form";

export const metadata: Metadata = { title: "Koleksiyon oluştur" };

// Burada otomatik kural sözleşmesi gelene kadar yalnız gerçek manuel koleksiyon oluşturma yüzeyini sunuyorum.
export default async function NewCollectionPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const params = await searchParams;
  await requireAdminPageSession("/collections/new");
  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title="Koleksiyon oluştur" description="Ürünleri elle seçerek yönetebileceğiniz bir koleksiyon oluşturun." backHref="/collections" />
      <CollectionError code={single(params.error)} />
      <CollectionForm action={createManualCollectionAction} mode="create" />
    </div>
  );
}

// Burada koleksiyon oluşturma hatalarını kullanıcıya güvenli ve eyleme dönük metinlerle açıklıyorum.
function CollectionError({ code }: { code?: string }) {
  if (!code) return null;
  const message = code === "conflict"
    ? "Bu bağlantı adresiyle zaten bir koleksiyon var."
    : code === "forbidden"
      ? "Bu işlem için yönetici yetkiniz bulunmuyor."
      : code === "session"
        ? "Oturumunuz sona erdi. Yeniden giriş yaptıktan sonra tekrar deneyin."
        : code === "validation"
          ? "Zorunlu alanları ve karakter sınırlarını kontrol edin."
          : "Koleksiyon kaydedilemedi. Lütfen tekrar deneyin.";
  return <p role="alert" className="mb-5 rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900">{message}</p>;
}

// Burada tekrarlı URL parametrelerinden ilk hata değerini seçiyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
