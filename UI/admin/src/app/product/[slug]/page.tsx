/* eslint-disable @next/next/no-img-element */
import type { Metadata } from "next";
import { notFound, permanentRedirect } from "next/navigation";
import { getProductBySlug } from "@/lib/products-api";
import {
  buildProductJsonLd,
  buildProductMetadata,
  productCanonicalUrl,
  serializeJsonLd,
} from "@/lib/product-seo";
import { siteConfig } from "@/lib/site-config";

type ProductPageProps = {
  params: Promise<{ slug: string }>;
};

async function resolveProduct(slug: string) {
  const data = await getProductBySlug(slug);
  if (!data) {
    notFound();
  }

  return data;
}

export async function generateMetadata({ params }: ProductPageProps): Promise<Metadata> {
  const { slug } = await params;
  const data = await resolveProduct(slug);
  return buildProductMetadata(data);
}

export default async function ProductPage({ params }: ProductPageProps) {
  const { slug } = await params;
  const data = await resolveProduct(slug);
  const { product, images } = data;

  if (slug !== product.url) {
    permanentRedirect(productCanonicalUrl(product.url));
  }

  const variants = product.variants.filter((variant) => variant.isActive);
  const prices = variants.map((variant) => variant.price);
  const minimumPrice = prices.length > 0 ? Math.min(...prices) : null;
  const hasStock = variants.some((variant) => variant.stock > 0);
  const priceFormatter = new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: siteConfig.currency,
  });

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(buildProductJsonLd(data)) }}
      />

      <article className="grid gap-10 lg:grid-cols-2">
        <section aria-label="Ürün görselleri" className="grid gap-4 sm:grid-cols-2">
          {images.length > 0 ? (
            images.map((image, index) => (
              <figure
                className={`overflow-hidden rounded-2xl bg-zinc-100 ${index === 0 ? "sm:col-span-2" : ""}`}
                key={image.id}
              >
                <img
                  src={image.imageUrl}
                  alt={image.altText || product.title}
                  className="aspect-square h-full w-full object-cover"
                  loading={index === 0 ? "eager" : "lazy"}
                  fetchPriority={index === 0 ? "high" : "auto"}
                />
              </figure>
            ))
          ) : (
            <div className="flex aspect-square items-center justify-center rounded-2xl bg-zinc-100 text-zinc-500 sm:col-span-2">
              Ürün görseli bulunmuyor
            </div>
          )}
        </section>

        <section>
          {product.brandName && (
            <p className="mb-2 text-sm font-medium uppercase tracking-wider text-zinc-500">
              {product.brandName}
            </p>
          )}
          <h1 className="text-4xl font-semibold tracking-tight text-zinc-950">{product.title}</h1>
          {minimumPrice !== null && (
            <p className="mt-6 text-2xl font-semibold text-zinc-900">
              {priceFormatter.format(minimumPrice)}
              {prices.some((price) => price !== minimumPrice) && " fiyatından başlayan"}
            </p>
          )}
          <p className={`mt-3 font-medium ${hasStock ? "text-emerald-700" : "text-red-700"}`}>
            {hasStock ? "Stokta" : "Stokta yok"}
          </p>
          {product.description && (
            <p className="mt-8 whitespace-pre-line leading-7 text-zinc-700">{product.description}</p>
          )}

          {variants.length > 0 && (
            <section aria-labelledby="variants-title" className="mt-8">
              <h2 id="variants-title" className="text-lg font-semibold text-zinc-950">
                Seçenekler
              </h2>
              <ul className="mt-3 grid gap-2">
                {variants.map((variant) => (
                  <li className="flex justify-between rounded-lg border border-zinc-200 p-3" key={variant.id}>
                    <span>{[variant.name, variant.value].filter(Boolean).join(" - ")}</span>
                    <span>{priceFormatter.format(variant.price)}</span>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </section>
      </article>
    </main>
  );
}
