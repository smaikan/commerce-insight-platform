import type { Metadata } from "next";

import { FavoritesPageClient } from "@/modules/favorites/components/favorites-page-client";
import { parseFavoritePage, parseFavoritePageSize } from "@/modules/favorites/request";

export const metadata: Metadata = {
  title: "Favorilerim",
  robots: { index: false, follow: false, noarchive: true },
};

type FavoritesPageProps = {
  searchParams: Promise<{ page?: string | string[]; pageSize?: string | string[] }>;
};

// Burada favorites rotasını hesap layout'undan ayırıp guest kullanıcıya da açık, noindex bir mağaza sayfası sunuyorum.
export default async function FavoritesPage({ searchParams }: FavoritesPageProps) {
  const query = await searchParams;
  const pageNumber = parseFavoritePage(query.page);
  const pageSize = parseFavoritePageSize(query.pageSize);

  return (
    <main id="main-content" className="flex-1 bg-background py-8 sm:py-12">
      <div className="page-shell">
        <FavoritesPageClient pageNumber={pageNumber} pageSize={pageSize} />
      </div>
    </main>
  );
}
