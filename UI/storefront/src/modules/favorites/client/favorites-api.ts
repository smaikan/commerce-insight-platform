"use client";

import type { FavoriteClientProblem, FavoriteProductPage, FavoriteState } from "@/modules/favorites/types";

const FAVORITES_UPDATED_EVENT = "storefront:favorites-updated";
const FAVORITES_SETTLED_EVENT = "storefront:favorites-settled";

let favoriteIds = new Set<string>();
let favoriteTotalCount = 0;
let favoriteStateLoaded = false;
let favoriteLoadPromise: Promise<FavoriteState> | null = null;
let favoriteStateRevision = 0;
const pendingFavoriteMutations = new Map<string, Promise<FavoriteState>>();

// Burada API'den gelen favori durumunu tek browser snapshot'ına yazıp tüm kalp kontrollerine yayıyorum.
function publishFavoriteState(state: FavoriteState): FavoriteState {
  favoriteIds = new Set(state.productIds);
  favoriteTotalCount = state.totalCount;
  favoriteStateLoaded = true;
  const snapshot = getFavoriteSnapshot();
  window.dispatchEvent(new CustomEvent<FavoriteState>(FAVORITES_UPDATED_EVENT, { detail: snapshot }));
  return snapshot;
}

// Burada liste henüz okunamamış olsa bile başarılı mutation sonucunu kalplere yayıp eksik snapshot'ı tam yüklenmiş saymıyorum.
function publishFavoriteMutation(productId: string, isFavorite: boolean): FavoriteState {
  const hadProduct = favoriteIds.has(productId);
  if (isFavorite) favoriteIds.add(productId);
  else favoriteIds.delete(productId);

  if (favoriteStateLoaded) {
    favoriteTotalCount = favoriteIds.size;
  } else if (isFavorite && !hadProduct) {
    favoriteTotalCount += 1;
  } else if (!isFavorite && hadProduct) {
    favoriteTotalCount = Math.max(0, favoriteTotalCount - 1);
  }

  const snapshot = getFavoriteSnapshot();
  favoriteStateRevision += 1;
  window.dispatchEvent(new CustomEvent<FavoriteState>(FAVORITES_UPDATED_EVENT, { detail: snapshot }));
  return snapshot;
}

// Burada same-origin favori cevabını güvenli typed sonuca veya istemci ProblemDetails hatasına ayırıyorum.
async function favoriteRequest<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    cache: "no-store",
    credentials: "same-origin",
  });
  const body = response.status === 204 ? null : await response.json().catch(() => null);

  if (!response.ok) {
    const source = body && typeof body === "object" ? body as Record<string, unknown> : {};
    throw {
      status: response.status,
      title: typeof source.title === "string" ? source.title : "Favori isteği tamamlanamadı",
      detail: typeof source.detail === "string" ? source.detail : undefined,
      code: typeof source.code === "string" ? source.code : undefined,
      traceId: typeof source.traceId === "string" ? source.traceId : undefined,
      retryAfter: response.headers.get("Retry-After") || undefined,
    } satisfies FavoriteClientProblem;
  }

  return (response.status === 204 ? undefined : body) as T;
}

// Burada navbar ve tüm ürün kalplerinin ilk favori okumasını tek promise üzerinden paylaşmasını sağlıyorum.
export function loadFavoriteState(force = false): Promise<FavoriteState> {
  if (favoriteLoadPromise) return favoriteLoadPromise;
  if (!force && favoriteStateLoaded) return Promise.resolve(getFavoriteSnapshot());

  const requestedAtRevision = favoriteStateRevision;
  const pending = favoriteRequest<FavoriteState>("/api/favorites", { method: "GET" })
    .then((state) => requestedAtRevision === favoriteStateRevision ? publishFavoriteState(state) : getFavoriteSnapshot());
  favoriteLoadPromise = pending;

  void pending.finally(() => {
    if (favoriteLoadPromise === pending) favoriteLoadPromise = null;
  }).catch(() => undefined);

  return pending;
}

// Burada favorites sayfasının ürün DTO'larını guest tokenını açmadan same-origin BFF'den sayfalı alıyorum.
export function loadFavoriteProducts(pageNumber: number, pageSize: number): Promise<FavoriteProductPage> {
  const query = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
  return favoriteRequest<FavoriteProductPage>(`/api/favorites/products?${query.toString()}`, { method: "GET" });
}

// Burada aynı ürüne paralel mutation göndermeden optimistic durumu 204 cevabıyla kesinleştiriyor, hatada önceki snapshot'a dönüyorum.
export function mutateFavorite(productId: string, shouldFavorite: boolean): Promise<FavoriteState> {
  const existingMutation = pendingFavoriteMutations.get(productId);
  if (existingMutation) return existingMutation;

  const wasFavorite = favoriteIds.has(productId);
  publishFavoriteMutation(productId, shouldFavorite);

  const pending = favoriteRequest<void>(
    `/api/favorites/${encodeURIComponent(productId)}`,
    { method: shouldFavorite ? "POST" : "DELETE" },
  )
    .then(() => getFavoriteSnapshot())
    .catch((error: unknown) => {
      // Burada duplicate eklemeyi backend'in authoritative favori=true kararı olarak koruyup diğer hataları rollback ediyorum.
      if (shouldFavorite && favoriteProblemStatus(error) === 409) {
        publishFavoriteMutation(productId, true);
      } else {
        publishFavoriteMutation(productId, wasFavorite);
      }
      throw error;
    })
    .finally(() => {
      if (pendingFavoriteMutations.get(productId) === pending) pendingFavoriteMutations.delete(productId);
      window.dispatchEvent(new Event(FAVORITES_SETTLED_EVENT));
    });

  pendingFavoriteMutations.set(productId, pending);
  return pending;
}

// Burada mevcut snapshot'ı dışarıya mutasyona kapalı bir dizi kopyasıyla veriyorum.
export function getFavoriteSnapshot(): FavoriteState {
  return { productIds: [...favoriteIds], totalCount: favoriteTotalCount };
}

// Burada bir ürünün favori durumunu ortak snapshot üzerinden sabit maliyetle okuyorum.
export function isFavoriteProduct(productId: string): boolean {
  return favoriteIds.has(productId);
}

// Burada navbar, kart ve detay kontrollerini tek özel browser olayına abone ediyorum.
export function subscribeToFavorites(listener: (state: FavoriteState) => void): () => void {
  // Burada custom event içindeki güncel snapshot'ı abone olan bileşene iletiyorum.
  function handleUpdate(event: Event) {
    listener((event as CustomEvent<FavoriteState>).detail);
  }

  window.addEventListener(FAVORITES_UPDATED_EVENT, handleUpdate);
  return () => window.removeEventListener(FAVORITES_UPDATED_EVENT, handleUpdate);
}

// Burada favorites sayfasının yalnız tamamlanmış mutation sonrasında authoritative ürün sayfasını yenilemesine izin veriyorum.
export function subscribeToFavoriteSettled(listener: () => void): () => void {
  window.addEventListener(FAVORITES_SETTLED_EVENT, listener);
  return () => window.removeEventListener(FAVORITES_SETTLED_EVENT, listener);
}

// Burada favori API hatalarını kullanıcıya doğru geri kazanım mesajıyla açıklıyorum.
export function favoriteErrorMessage(error: unknown): string {
  if (!error || typeof error !== "object") return "Favori işlemi tamamlanamadı. Lütfen tekrar deneyin.";

  const problem = error as Partial<FavoriteClientProblem>;
  if (problem.status === 401) return "Favori oturumu kurulamadı. Lütfen tekrar deneyin.";
  if (problem.status === 403) return "Favori isteğinin güvenlik doğrulaması tamamlanamadı.";
  if (problem.status === 409) return "Ürün zaten favorilerinizde.";
  if (problem.status === 404) return "Ürün veya favori kaydı artık bulunamıyor.";
  if (problem.status === 429) return "Çok fazla istek gönderildi. Lütfen kısa bir süre bekleyin.";
  if (problem.status === 400) return problem.detail || "Favori isteği geçerli değil.";
  return "Favori işlemi tamamlanamadı. Lütfen tekrar deneyin.";
}

// Burada ProblemDetails durum kodunu 401, 404 ve 409 uzlaştırmalarında tek güvenli yerden okuyorum.
export function favoriteProblemStatus(error: unknown): number | null {
  if (!error || typeof error !== "object") return null;
  const status = Number((error as Partial<FavoriteClientProblem>).status);
  return Number.isInteger(status) ? status : null;
}

// Burada mutation sonrasında oturumu yenileme akışına alınması gereken 401 cevabını ayırıyorum.
export function isFavoriteAuthenticationError(error: unknown): boolean {
  return Boolean(error && typeof error === "object" && (error as Partial<FavoriteClientProblem>).status === 401);
}

// Burada test ve tam oturum değişimi sonrasında client snapshot'ını güvenle sıfırlıyorum.
export function resetFavoriteState(): void {
  favoriteIds = new Set<string>();
  favoriteTotalCount = 0;
  favoriteStateLoaded = false;
  favoriteLoadPromise = null;
  favoriteStateRevision += 1;
  pendingFavoriteMutations.clear();
}

// Burada login veya logout ile owner değiştiğinde önceki oturuma ait bellek snapshot'ını yeni oturuma taşımıyorum.
export function clearFavoriteStateForOwnerChange(): void {
  if (!favoriteStateLoaded && favoriteIds.size === 0 && favoriteTotalCount === 0) return;
  resetFavoriteState();
  window.dispatchEvent(new CustomEvent<FavoriteState>(FAVORITES_UPDATED_EVENT, { detail: getFavoriteSnapshot() }));
}
