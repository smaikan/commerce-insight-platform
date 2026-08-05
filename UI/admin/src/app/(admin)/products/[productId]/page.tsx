import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getProduct, getProductFormOptions, getProductImages } from "@/modules/products/api";
import { ProductForm } from "@/modules/products/components/product-form";

// Burada ürün kimliğini metadata başlığında güvenli bağlam olarak gösteriyorum.
export async function generateMetadata({ params }: { params: Promise<{ productId: string }> }): Promise<Metadata> {
  const { productId } = await params;
  return { title: `Ürün ${productId}` };
}

// Burada ürün, görseller ve form seçeneklerini birbirinden bağımsız olarak paralel getiriyorum.
export default async function EditProductPage({
  params,
  searchParams,
}: {
  params: Promise<{ productId: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const { productId } = await params;
  const notices = await searchParams;
  const returnTo = `/products/${encodeURIComponent(productId)}`;
  const session = await requireAdminPageSession(returnTo);

  let data;
  try {
    data = await Promise.all([
      getProduct(productId, session),
      getProductImages(productId, session),
      getProductFormOptions(session),
    ]);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  const [product, imagePage, options] = data;
  return (
    <div className="mx-auto w-full max-w-7xl">
      <PageHeader
        title={product.title}
        description={`${product.id} · ${product.mainSku}`}
        backHref="/products"
      />
      {notices.created === "1" ? (
        <p className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900" role="status">Ürün başarıyla oluşturuldu.</p>
      ) : notices.saved === "1" ? (
        <p className="mb-5 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-900" role="status">Ürün değişiklikleri kaydedildi.</p>
      ) : null}
      <ProductForm mode="edit" product={product} images={imagePage.items} options={options} />
    </div>
  );
}
