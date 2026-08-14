"use client";

import { isSearchQueryValid, normalizeSearchQuery } from "@/modules/search/query";

type DebouncedSearchOptions<T> = {
  query: string;
  delayMs: number;
  request: (query: string, signal: AbortSignal) => Promise<T>;
  onReset: () => void;
  onStart: () => void;
  onSuccess: (result: T) => void;
  onError: (error: unknown) => void;
};

// Burada debounce, AbortController ve geç cevap korumasını tek iptal edilebilir görevde birleştiriyorum.
export function scheduleDebouncedSearch<T>(options: DebouncedSearchOptions<T>): () => void {
  const normalizedQuery = normalizeSearchQuery(options.query);
  if (!isSearchQueryValid(normalizedQuery)) {
    options.onReset();
    return () => undefined;
  }

  let active = true;
  const controller = new AbortController();
  options.onStart();
  const timer = setTimeout(async () => {
    try {
      const result = await options.request(normalizedQuery, controller.signal);
      if (active && !controller.signal.aborted) options.onSuccess(result);
    } catch (error) {
      if (active && !controller.signal.aborted) options.onError(error);
    }
  }, options.delayMs);

  // Burada sorgu değişimi, modal kapanışı veya unmount anında hem bekleyen timer'ı hem aktif HTTP isteğini durduruyorum.
  return () => {
    active = false;
    clearTimeout(timer);
    controller.abort();
  };
}
