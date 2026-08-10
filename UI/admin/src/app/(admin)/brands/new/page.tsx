import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { BrandForm } from "@/modules/brands/components/brand-form";

export const metadata: Metadata = { title: "Marka oluştur" };

// Burada yeni marka bilgilerini ve opsiyonel görselini tek görev odaklı formda topluyorum.
export default async function NewBrandPage() {
  await requireAdminPageSession("/brands/new");
  return (
    <div className="mx-auto w-full max-w-5xl">
      <PageHeader title="Marka oluştur" description="Ürünlerde kullanılacak marka kimliğini ve opsiyonel görselini ekleyin." backHref="/brands" />
      <BrandForm />
    </div>
  );
}
