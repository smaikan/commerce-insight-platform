import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ApiError } from "@/lib/api/problem";
import {
  buildProductBreadcrumbJsonLd,
  buildProductJsonLd,
  buildProductMetadata,
  serializeJsonLd,
} from "@/lib/product-seo";
import { getPublishedProductBySlug } from "@/modules/product/api";
import { orderProductImages, ProductGallery } from "@/modules/product/components/product-gallery";
import { ProductSummary } from "@/modules/product/components/product-summary";
import { ProductViewTracker } from "@/modules/product/components/product-view-tracker";

type ProductPageProps = {
  params: Promise<{ slug: string }>;
};

// Burada eksik veya yayında olmayan ürünü indexlenebilir metadata üretmeden 404 akışına taşıyorum.
async function resolvePublishedProduct(slug: string) {
  try {
    return await getPublishedProductBySlug(slug);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    throw error;
  }
}

// Burada ürün metadata ve sayfa içeriğinin aynı request-scope ürün fetch'ini paylaşmasını sağlıyorum.
export async function generateMetadata({ params }: ProductPageProps): Promise<Metadata> {
  const { slug } = await params;
  return buildProductMetadata(await resolvePublishedProduct(slug));
}

// Burada referanstaki geniş medya ve sabit bilgi paneli kompozisyonunu gerçek ürün sözleşmesine uyarlıyorum.
export default async function ProductPage({ params }: ProductPageProps) {
  const { slug } = await params;
  const data = await resolvePublishedProduct(slug);
  const images = orderProductImages(data.images);

  return (
    <main id="main-content" className="page-shell flex-1 py-6 sm:py-8 lg:py-10">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(buildProductJsonLd(data)) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(buildProductBreadcrumbJsonLd(data)) }}
      />
      <nav aria-label="İçerik yolu" className="mb-8 hidden text-xs font-semibold text-ink-muted md:block">
        <ol className="flex flex-wrap items-center gap-2">
          <li><Link className="nav-link" href="/" prefetch={false}>Ana sayfa</Link></li>
          <li aria-hidden="true">/</li>
          <li><Link className="nav-link" href="/products" prefetch={false}>Ürünler</Link></li>
          <li aria-hidden="true">/</li>
          <li aria-current="page" className="line-clamp-1">{data.product.title}</li>
        </ol>
      </nav>
      <ProductViewTracker productId={data.product.id} />
      <div className="grid w-full min-w-0 items-start gap-8 lg:grid-cols-[minmax(0,1.1fr)_minmax(24rem,0.9fr)] lg:gap-x-10 xl:gap-x-14">
        <ProductGallery images={images} productTitle={data.product.title} />
        <ProductSummary product={data.product} />
      </div>
    </main>
  );
}
