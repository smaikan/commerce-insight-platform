import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getProductDailyMetrics } from "@/modules/analytics/api";
import { AnalyticsUnavailable, ProductAnalyticsPanel } from "@/modules/analytics/components/analytics-panels";
import { getAnalyticsDateRange, parseAnalyticsPeriod } from "@/modules/analytics/query";
import { getProduct, getProductFormOptions, getProductImages } from "@/modules/products/api";
import { ProductEditWorkspace } from "@/modules/products/components/product-edit-workspace";

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
  const selectedPeriod = parseAnalyticsPeriod(notices);
  const range = getAnalyticsDateRange(selectedPeriod);
  const returnTo = `/products/${encodeURIComponent(productId)}`;
  const session = await requireAdminPageSession(returnTo);

  let data;
  let metrics: Awaited<ReturnType<typeof getProductDailyMetrics>> | null = null;
  try {
    const [baseData, metricResult] = await Promise.all([
      Promise.all([
        getProduct(productId, session),
        getProductImages(productId, session),
        getProductFormOptions(session),
      ]),
      getProductDailyMetrics(productId, range, session).catch(() => null),
    ]);
    data = baseData;
    metrics = metricResult;
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }

  const [product, imagePage, options] = data;
  return (
    <div className="mx-auto w-full max-w-6xl">
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
      {/* Burada ürün kaydı ve ürün bağlamlı stok hareketini ayrı formlarla aynı çalışma alanında sunuyorum. */}
      <ProductEditWorkspace
        product={product}
        images={imagePage.items}
        options={options}
      />
      {/* Burada performans analizini düzenleme akışından ayırıp sayfanın en altındaki inceleme alanına taşıyorum. */}
      <div className="mt-4">
        {metrics ? (
          <ProductAnalyticsPanel metrics={metrics} selectedPeriod={selectedPeriod} searchParams={notices} productId={productId} />
        ) : (
          <AnalyticsUnavailable />
        )}
      </div>
    </div>
  );
}
