import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getProductFormOptions } from "@/modules/products/api";
import { ProductForm } from "@/modules/products/components/product-form";

export const metadata: Metadata = { title: "Yeni Ürün" };

// Burada yeni ürün formunun marka ve vergi seçeneklerini sunucuda hazırlıyorum.
export default async function NewProductPage() {
  const session = await requireAdminPageSession("/products/new");
  const options = await getProductFormOptions(session);

  return (
    <div className="mx-auto w-full max-w-7xl">
      <PageHeader
        title="Yeni ürün"
        description="Temel bilgiler, durum, organizasyon ve en az bir varyantla katalog kaydı oluşturun."
        backHref="/products"
      />
      <ProductForm mode="create" options={options} />
    </div>
  );
}
