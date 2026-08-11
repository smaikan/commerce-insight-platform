import { permanentRedirect } from "next/navigation";

// Burada eski tekil product yolunu yeni canonical çoğul ürün yoluna kalıcı olarak yönlendiriyorum.
export default async function LegacyProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  permanentRedirect(`/products/${encodeURIComponent(slug)}`);
}
