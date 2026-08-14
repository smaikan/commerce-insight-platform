import type { Metadata } from "next";

// Burada yasal sayfaların başlık, açıklama, canonical ve Open Graph sinyallerini aynı sayfa niyetinde tutuyorum.
export function legalPageMetadata(title: string, description: string, path: string): Metadata {
  return {
    title,
    description,
    alternates: { canonical: path },
    robots: { index: true, follow: true },
    openGraph: { type: "website", title, description, url: path },
  };
}
