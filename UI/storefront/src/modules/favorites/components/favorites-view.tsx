import Link from "next/link";

import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import { FavoriteProductCard } from "@/modules/favorites/components/favorite-product-card";
import type { FavoriteProductPage } from "@/modules/favorites/types";

// Burada favoriler hesabını gerçek sayfalı ürünler, açık boş durum ve katalog dönüşüyle sunuyorum.
export function FavoritesView({ products }: { products: FavoriteProductPage }) {
  return (
    <section>
      <AccountPageHeader
        eyebrow="Kaydedilen ürünler"
        title="Favorilerim"
        description="Beğendiğiniz ürünleri burada saklayabilir, ürün detayına hızlıca dönebilirsiniz."
        action={<Link href="/products" className="focus-ring inline-flex min-h-11 items-center border border-line bg-surface px-4 text-sm font-bold text-ink hover:border-brand-600 hover:text-brand-700">Ürünleri keşfet</Link>}
      />

      {products.items.length > 0 ? (
        <>
          <p className="mt-6 text-sm text-ink-muted"><span className="font-bold text-ink">{products.totalCount}</span> kayıtlı ürün</p>
          <div className="mt-5 grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-4 md:grid-cols-3 xl:grid-cols-4 xl:gap-x-6" aria-label="Favori ürünler">
            {products.items.map((product) => <FavoriteProductCard key={product.id} product={product} />)}
          </div>
          <FavoritePagination
            page={products.pageNumber}
            pageSize={products.pageSize}
            totalPages={products.totalPages}
            hasPreviousPage={products.hasPreviousPage}
            hasNextPage={products.hasNextPage}
          />
        </>
      ) : (
        <div className="mt-8 border border-line bg-surface px-6 py-12 text-center">
          <h2 className="text-xl font-bold text-brand-950">Henüz favori ürününüz yok</h2>
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-ink-muted">Beğendiğiniz ürünlerdeki kalp simgesine dokunarak onları bu alanda saklayabilirsiniz.</p>
          <Link href="/products" className="focus-ring mt-6 inline-flex min-h-11 items-center bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700">Kataloğa git</Link>
        </div>
      )}
    </section>
  );
}

// Burada API'nin sayfalama bayraklarını kaynak kabul edip sayfa boyutunu önceki ve sonraki URL'lerde koruyorum.
function FavoritePagination({
  page,
  pageSize,
  totalPages,
  hasPreviousPage,
  hasNextPage,
}: {
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}) {
  if (totalPages <= 1) return null;

  return (
    <nav className="mt-12 flex items-center justify-between border-t border-line pt-6" aria-label="Favori ürün sayfaları">
      {hasPreviousPage ? <Link className="focus-ring border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600" href={`/account/favorites?page=${page - 1}&pageSize=${pageSize}`} rel="prev">Önceki</Link> : <span />}
      <p className="text-sm text-ink-muted"><span className="font-semibold text-ink">{page}</span> / {totalPages}</p>
      {hasNextPage ? <Link className="focus-ring border border-line bg-surface px-4 py-2.5 text-sm font-semibold hover:border-brand-600" href={`/account/favorites?page=${page + 1}&pageSize=${pageSize}`} rel="next">Sonraki</Link> : <span />}
    </nav>
  );
}
