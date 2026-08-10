import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getBrand } from "@/modules/brands/api";
import { BrandForm } from "@/modules/brands/components/brand-form";

export const metadata: Metadata = { title: "Marka düzenle" };

// Burada marka detayını yetkili kaynaktan okuyup bilgi ve görsel düzenleme formuna bağlıyorum.
export default async function EditBrandPage({ params, searchParams }: { params: Promise<{ brandId: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const [{ brandId }, query] = await Promise.all([params, searchParams]);
  const session = await requireAdminPageSession(`/brands/${encodeURIComponent(brandId)}`);
  let brand;
  try {
    brand = await getBrand(brandId, session);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title="Markayı düzenle" description={brand.name} backHref="/brands" />
      <PartialImageNotice notice={single(query.notice)} />
      <BrandForm brand={brand} />
    </div>
  );
}

// Burada kayıt oluşturulup görsel adımı tamamlanamadığında kısmi başarıyı açıkça bildiriyorum.
function PartialImageNotice({ notice }: { notice?: string }) {
  if (notice !== "image-upload-failed" && notice !== "image-attach-failed") return null;
  const detail = notice === "image-upload-failed" ? "Görsel yüklenemedi." : "Yüklenen görsel markaya bağlanamadı.";
  return <p role="alert" className="mb-5 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900"><strong>Marka oluşturuldu.</strong> {detail} Bu ekrandan tekrar görsel seçebilirsiniz.</p>;
}

// Burada tekrarlı URL parametrelerinden ilk bildirim değerini seçiyorum.
function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
