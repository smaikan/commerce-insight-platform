"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";

import { formatCurrency } from "@/lib/formatting/currency";
import {
  isSearchRateLimited,
  requestSearchInspiration,
  requestSearchSuggestions,
  searchErrorMessage,
} from "@/modules/search/client/search-api";
import { scheduleDebouncedSearch } from "@/modules/search/client/search-request";
import {
  isSearchQueryValid,
  normalizeSearchQuery,
  searchResultsHref,
} from "@/modules/search/query";
import type { SearchProduct, SearchSuggestions } from "@/modules/search/types";

const SEARCH_DEBOUNCE_MS = 250;

type RequestStatus = "idle" | "loading" | "success" | "error" | "rate-limit";

// Burada test fixture'larını yalnız sunum başlangıcı olarak kabul edip wire modelini OpenAPI tipinden koruyorum.
type SearchOverlayProps = {
  initialQuery?: string;
  initialResults?: SearchProduct[];
  initialHasMore?: boolean;
  initialInspiration?: SearchProduct[];
};

// Burada tam ekran arama modalının etkileşim ve ağ durumunu navbar'daki küçük client sınırında tutuyorum.
export function SearchOverlay({
  initialQuery = "",
  initialResults = [],
  initialHasMore = false,
  initialInspiration = [],
}: SearchOverlayProps = {}) {
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState(initialQuery);
  const [results, setResults] = useState(initialResults);
  const [hasMore, setHasMore] = useState(initialHasMore);
  const [searchStatus, setSearchStatus] = useState<RequestStatus>(initialResults.length ? "success" : "idle");
  const [searchError, setSearchError] = useState("");
  const [searchRevision, setSearchRevision] = useState(0);
  const [inspiration, setInspiration] = useState(initialInspiration);
  const [inspirationStatus, setInspirationStatus] = useState<RequestStatus>(initialInspiration.length ? "success" : "idle");
  const [inspirationError, setInspirationError] = useState("");
  const [inspirationRevision, setInspirationRevision] = useState(0);
  const inspirationRequestedRef = useRef(initialInspiration.length > 0);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const normalizedQuery = normalizeSearchQuery(query);
  const canSearch = isSearchQueryValid(normalizedQuery);

  // Burada native dialog top-layer davranışını React durumuyla eşitleyip açılış odağını arama alanına taşıyorum.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (isOpen && !dialog.open) {
      dialog.showModal();
      window.requestAnimationFrame(() => inputRef.current?.focus());
    }

    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);

  // Burada modal ilk açıldığında ilham ürünlerini yalnız bir kez yükleyip kapanışta aktif isteği iptal ediyorum.
  useEffect(() => {
    if (!isOpen || inspirationRequestedRef.current) return;

    const controller = new AbortController();
    let settled = false;
    inspirationRequestedRef.current = true;
    setInspirationStatus("loading");
    void requestSearchInspiration(controller.signal)
      .then((response) => {
        if (controller.signal.aborted) return;
        settled = true;
        setInspiration(response.items);
        setInspirationStatus("success");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return;
        settled = true;
        setInspirationError(searchErrorMessage(error));
        setInspirationStatus(isSearchRateLimited(error) ? "rate-limit" : "error");
      });

    return () => {
      if (!settled) {
        controller.abort();
        inspirationRequestedRef.current = false;
      }
    };
  }, [inspirationRevision, isOpen]);

  // Burada sorgu değişimini 250 ms debounce, gerçek HTTP iptali ve geç response korumasıyla suggestion endpointine bağlıyorum.
  useEffect(() => {
    if (!isOpen) return;

    return scheduleDebouncedSearch<SearchSuggestions>({
      query,
      delayMs: SEARCH_DEBOUNCE_MS,
      request: requestSearchSuggestions,
      onReset: () => {
        setResults([]);
        setHasMore(false);
        setSearchError("");
        setSearchStatus("idle");
      },
      onStart: () => {
        setResults([]);
        setHasMore(false);
        setSearchError("");
        setSearchStatus("loading");
      },
      onSuccess: (response) => {
        setResults(response.items.slice(0, 10));
        setHasMore(response.hasMore);
        setSearchStatus("success");
      },
      onError: (error) => {
        setSearchError(searchErrorMessage(error));
        setSearchStatus(isSearchRateLimited(error) ? "rate-limit" : "error");
      },
    });
  }, [isOpen, query, searchRevision]);

  // Burada native close veya Escape sonrasında geçici arama durumunu temizleyip odağı büyüteç düğmesine geri veriyorum.
  function handleDialogClose() {
    setIsOpen(false);
    setQuery("");
    setResults([]);
    setHasMore(false);
    setSearchError("");
    setSearchStatus("idle");
    triggerRef.current?.focus();
  }

  // Burada kullanıcı Enter'a bastığında ayrı bir Ara düğmesi oluşturmadan tam katalog sonucuna geçiyorum.
  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSearch) return;
    dialogRef.current?.close();
    router.push(searchResultsHref(normalizedQuery));
  }

  // Burada linkin varsayılan navigasyonunu tamamlamasına izin verip modalı bir sonraki görevde kapatıyorum.
  function handleResultNavigation() {
    window.setTimeout(() => dialogRef.current?.close(), 0);
  }

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        className="header-action inline-flex size-11 cursor-pointer items-center justify-center p-0 hover:bg-surface-subtle"
        aria-label="Ürün ara"
        aria-haspopup="dialog"
        aria-expanded={isOpen}
        onClick={() => setIsOpen(true)}
      >
        <SearchIcon />
      </button>

      <dialog
        ref={dialogRef}
        className="search-dialog fixed inset-0 m-0 h-dvh max-h-none w-screen max-w-none overflow-hidden border-0 bg-background p-0 text-ink"
        aria-labelledby="search-dialog-title"
        onClose={handleDialogClose}
      >
        <div className="flex h-full min-h-0 flex-col">
          <header className="shrink-0 border-b border-line bg-surface">
            <div className="page-shell grid min-h-24 grid-cols-[1fr_auto] items-center gap-4 py-5 sm:min-h-32 sm:grid-cols-[10rem_minmax(0,1fr)_3rem] sm:gap-7 lg:grid-cols-[16rem_minmax(0,1fr)_3rem] lg:gap-10">
              <h1 id="search-dialog-title" className="hidden text-sm font-black tracking-[0.12em] text-brand-950 uppercase sm:block lg:text-base">
                Sitede ara
              </h1>

              <form role="search" className="min-w-0" onSubmit={handleSubmit}>
                <label htmlFor="storefront-search-input" className="sr-only">Ürün ara</label>
                <div className="relative">
                  <input
                    ref={inputRef}
                    id="storefront-search-input"
                    type="search"
                    value={query}
                    maxLength={100}
                    onChange={(event) => setQuery(event.target.value)}
                    placeholder="Ürün adı, marka veya kategori ara"
                    autoComplete="off"
                    enterKeyHint="search"
                    className="focus-ring h-13 w-full rounded-full border border-line bg-surface pr-13 pl-5 text-base text-ink placeholder:text-ink-muted/75 sm:h-15 sm:px-7 sm:pr-15 sm:text-lg"
                    aria-describedby="storefront-search-help"
                  />
                  <span className="pointer-events-none absolute inset-y-0 right-4 flex items-center text-brand-700 sm:right-5">
                    <SearchIcon />
                  </span>
                </div>
                <p id="storefront-search-help" className="mt-3 text-xs leading-5 text-ink-muted sm:text-sm">
                  En az iki karakter yazın; ürünler otomatik olarak listelenecek.
                </p>
              </form>

              <button
                type="button"
                className="focus-ring inline-flex size-11 items-center justify-center justify-self-end text-ink hover:text-brand-700"
                aria-label="Aramayı kapat"
                onClick={() => dialogRef.current?.close()}
              >
                <CloseIcon />
              </button>
            </div>
          </header>

          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain">
            <div className="page-shell py-7 sm:py-9 lg:py-10">
              {canSearch ? (
                <SearchResultState
                  query={normalizedQuery}
                  products={results}
                  hasMore={hasMore}
                  status={searchStatus}
                  error={searchError}
                  onRetry={() => setSearchRevision((value) => value + 1)}
                  onNavigate={handleResultNavigation}
                />
              ) : (
                <SearchInspirationState
                  products={inspiration}
                  status={inspirationStatus}
                  error={inspirationError}
                  onRetry={() => {
                    inspirationRequestedRef.current = false;
                    setInspirationStatus("idle");
                    setInspirationRevision((value) => value + 1);
                  }}
                  onNavigate={handleResultNavigation}
                />
              )}
            </div>
          </div>
        </div>
      </dialog>
    </>
  );
}

// Burada canlı aramanın loading, hata, rate-limit, boş ve dolu durumlarını aynı sabit sonuç geometrisinde sunuyorum.
function SearchResultState({
  query,
  products,
  hasMore,
  status,
  error,
  onRetry,
  onNavigate,
}: {
  query: string;
  products: SearchProduct[];
  hasMore: boolean;
  status: RequestStatus;
  error: string;
  onRetry: () => void;
  onNavigate: () => void;
}) {
  if (status === "loading" || status === "idle") return <SearchLoading count={10} />;
  if (status === "error" || status === "rate-limit") {
    return <SearchErrorState message={error} canRetry={status !== "rate-limit"} onRetry={onRetry} />;
  }
  return <SearchResults query={query} products={products} hasMore={hasMore} onNavigate={onNavigate} />;
}

// Burada modal ilk açılışını tek sıralı popüler ürün vitriniyle doldurup her state'i aynı başlık altında koruyorum.
function SearchInspirationState({
  products,
  status,
  error,
  onRetry,
  onNavigate,
}: {
  products: SearchProduct[];
  status: RequestStatus;
  error: string;
  onRetry: () => void;
  onNavigate: () => void;
}) {
  if (status === "loading" || status === "idle") return <SearchLoading count={5} title="Biraz ilhama mı ihtiyacınız var?" singleRow />;
  if (status === "error" || status === "rate-limit") {
    return <SearchErrorState message={error} canRetry={status !== "rate-limit"} onRetry={onRetry} title="Biraz ilhama mı ihtiyacınız var?" />;
  }
  return <SearchInspiration products={products} onNavigate={onNavigate} />;
}

// Burada ilham ürünlerini masaüstünde beşli, mobilde yatay kaydırılabilir tek sıra olarak sunuyorum.
export function SearchInspiration({ products, onNavigate }: { products: SearchProduct[]; onNavigate?: () => void }) {
  return (
    <section aria-labelledby="search-inspiration-title">
      <h2 id="search-inspiration-title" className="text-2xl font-semibold tracking-[-0.03em] text-brand-950 sm:text-3xl">
        Biraz ilhama mı ihtiyacınız var?
      </h2>
      {products.length ? (
        <ul className="search-product-grid mt-7 grid grid-flow-col auto-cols-[minmax(12rem,72vw)] gap-4 overflow-x-auto pb-3 sm:auto-cols-[minmax(13rem,42vw)] lg:grid-flow-row lg:grid-cols-5 lg:auto-cols-auto lg:overflow-visible lg:pb-0">
          {products.slice(0, 5).map((product) => (
            <li key={product.id}>
              <SearchProductCard product={product} onNavigate={onNavigate} />
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-6 border-y border-line py-10 text-sm text-ink-muted" role="status">
          İlham ürünleri şu anda görüntülenemiyor. Arama alanına yazarak ürünleri keşfedebilirsiniz.
        </p>
      )}
    </section>
  );
}

// Burada suggestion sonucunu backend sırasını bozmadan en fazla on kartlık ve iki satırlık vitrin olarak sunuyorum.
export function SearchResults({
  query,
  products,
  hasMore,
  onNavigate,
}: {
  query: string;
  products: SearchProduct[];
  hasMore: boolean;
  onNavigate?: () => void;
}) {
  if (products.length === 0) {
    return (
      <section className="flex min-h-64 items-center justify-center border-y border-line/80 py-12 text-center" role="status">
        <div className="max-w-md">
          <h2 className="text-xl font-bold text-brand-950">Eşleşen ürün bulunamadı</h2>
          <p className="mt-2 text-sm leading-6 text-ink-muted">Yazımı kontrol edin veya daha kısa bir arama deneyin.</p>
        </div>
      </section>
    );
  }

  const visibleProducts = products.slice(0, 10);
  const allResultsHref = searchResultsHref(query);

  return (
    <section aria-labelledby="search-products-title">
      <p className="sr-only" role="status">
        {visibleProducts.length} ürün gösteriliyor{hasMore ? ", daha fazla sonuç var" : ""}.
      </p>
      <div className="mb-7 flex items-end justify-between gap-5 sm:mb-8">
        <div>
          <p className="text-xs font-bold tracking-[0.12em] text-brand-700 uppercase">Arama sonucu</p>
          <h2 id="search-products-title" className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-brand-950 sm:text-3xl">Ürünler</h2>
        </div>
        <Link href={allResultsHref} prefetch={false} onClick={onNavigate} className="focus-ring shrink-0 border-b border-brand-950 pb-1 text-sm font-semibold text-ink-muted hover:text-brand-700 sm:text-base">
          Tümünü gör <span aria-hidden="true">↗</span>
        </Link>
      </div>

      <ul className="search-product-grid grid grid-flow-col grid-rows-2 auto-cols-[minmax(10rem,44vw)] gap-x-3 gap-y-7 overflow-x-auto pb-3 sm:auto-cols-[minmax(12rem,30vw)] sm:gap-x-5 lg:grid-flow-row lg:grid-cols-5 lg:grid-rows-none lg:auto-cols-auto lg:overflow-visible lg:pb-0">
        {visibleProducts.map((product) => (
          <li key={product.id}>
            <SearchProductCard product={product} onNavigate={onNavigate} />
          </li>
        ))}
      </ul>
    </section>
  );
}

// Burada sonuç geometrisini sabit tutan sade skeleton'larla yükleme durumunda yer kaymasını önlüyorum.
function SearchLoading({ count, title, singleRow = false }: { count: number; title?: string; singleRow?: boolean }) {
  return (
    <section aria-busy="true" aria-live="polite">
      {title ? <h2 className="text-2xl font-semibold tracking-[-0.03em] text-brand-950 sm:text-3xl">{title}</h2> : <h2 className="sr-only">Arama sonuçları yükleniyor</h2>}
      <p className="sr-only">Ürünler yükleniyor.</p>
      <ul className={`search-product-grid ${title ? "mt-7" : ""} grid grid-flow-col ${singleRow ? "grid-rows-1" : "grid-rows-2"} auto-cols-[minmax(10rem,44vw)] gap-x-3 gap-y-7 overflow-x-auto pb-3 sm:auto-cols-[minmax(12rem,30vw)] lg:grid-flow-row lg:grid-cols-5 lg:grid-rows-none lg:auto-cols-auto lg:overflow-visible lg:pb-0`} aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <li key={index}>
            <div className="aspect-[4/5] rounded-xl border border-line/70 bg-surface-subtle" />
            <div className="mt-3 h-3 w-1/3 rounded bg-surface-subtle" />
            <div className="mt-2 h-4 w-4/5 rounded bg-surface-subtle" />
            <div className="mt-3 h-4 w-1/2 rounded bg-surface-subtle" />
          </li>
        ))}
      </ul>
    </section>
  );
}

// Burada rate-limit ve genel bağlantı hatasını otomatik retry yapmadan açıklayıp yalnız uygun durumda manuel tekrar sunuyorum.
function SearchErrorState({ message, canRetry, onRetry, title }: { message: string; canRetry: boolean; onRetry: () => void; title?: string }) {
  return (
    <section aria-labelledby="search-error-title">
      {title ? <h2 className="text-2xl font-semibold tracking-[-0.03em] text-brand-950 sm:text-3xl">{title}</h2> : null}
      <div className={`${title ? "mt-7" : ""} flex min-h-56 items-center justify-center border-y border-line/80 py-12 text-center`} role="alert">
        <div className="max-w-md">
          <h2 id="search-error-title" className="text-lg font-bold text-brand-950">Ürünler yüklenemedi</h2>
          <p className="mt-2 text-sm leading-6 text-ink-muted">{message}</p>
          {canRetry ? (
            <button type="button" className="focus-ring mt-5 rounded-lg bg-brand-700 px-4 py-2.5 text-sm font-bold text-white hover:bg-brand-950" onClick={onRetry}>
              Tekrar dene
            </button>
          ) : null}
        </div>
      </div>
    </section>
  );
}

// Burada modal kartını 4:5 görsel, ürün kimliği ve fiyat odağında hafif bir link olarak kuruyorum.
function SearchProductCard({ product, onNavigate }: { product: SearchProduct; onNavigate?: () => void }) {
  const hasDiscount = product.price !== null
    && product.price !== undefined
    && product.compareAtPrice !== null
    && product.compareAtPrice !== undefined
    && product.compareAtPrice > product.price;

  return (
    <article className="group min-w-0">
      <Link href={`/products/${encodeURIComponent(product.url)}`} prefetch={false} onClick={onNavigate} className="focus-ring block">
        <div className="relative aspect-[4/5] overflow-hidden rounded-xl border border-line/70 bg-surface-subtle">
          {product.imageUrl ? (
            <Image
              src={product.imageUrl}
              alt={product.imageAlt || product.title}
              fill
              loading="lazy"
              className="object-cover transition-transform duration-300 group-hover:scale-[1.015]"
              sizes="(min-width: 1024px) 18vw, (min-width: 640px) 30vw, 44vw"
            />
          ) : (
            <div className="flex size-full items-center justify-center px-5 text-center text-xs text-ink-muted sm:text-sm">Ürün görseli bulunmuyor</div>
          )}
        </div>

        <div className="px-0.5 pt-3">
          {product.brandName ? <p className="truncate text-xs font-semibold tracking-[0.04em] text-brand-700">{product.brandName}</p> : null}
          <h3 className="mt-1 line-clamp-2 text-sm font-semibold leading-5 text-ink transition-colors group-hover:text-brand-700 sm:text-[0.9375rem]">{product.title}</h3>
          <div className="mt-2 flex flex-wrap items-baseline gap-x-2 gap-y-1">
            {product.price !== null && product.price !== undefined ? <span className="text-sm font-bold text-ink sm:text-base">{formatCurrency(product.price)}</span> : <span className="text-xs text-ink-muted">Fiyat bilgisi yok</span>}
            {hasDiscount ? <span className="text-xs text-ink-muted line-through">{formatCurrency(product.compareAtPrice!)}</span> : null}
          </div>
          {!product.isAvailable ? <p className="mt-1.5 text-xs font-semibold text-danger">Şu an mevcut değil</p> : null}
        </div>
      </Link>
    </article>
  );
}

// Burada arama eylemini ortak çizgi kalınlığına sahip hafif bir SVG ile gösteriyorum.
function SearchIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round">
      <circle cx="11" cy="11" r="6.5" />
      <path d="m16 16 4 4" />
    </svg>
  );
}

// Burada modal kapatma eylemini metin dışı ve yardımcı teknolojiden gizli bir SVG ile gösteriyorum.
function CloseIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="size-7" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round">
      <path d="M5 5l14 14M19 5 5 19" />
    </svg>
  );
}
