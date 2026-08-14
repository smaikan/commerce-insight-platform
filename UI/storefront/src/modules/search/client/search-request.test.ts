import { afterEach, describe, expect, it, vi } from "vitest";

import { scheduleDebouncedSearch } from "./search-request";

afterEach(() => {
  vi.useRealTimers();
});

describe("debounced search request", () => {
  // Burada iki karakterden kısa sorgunun timer veya HTTP görevi üretmediğini doğruluyorum.
  it("does not request for a query shorter than two characters", async () => {
    vi.useFakeTimers();
    const request = vi.fn(async () => ({ items: [] }));
    const onReset = vi.fn();

    scheduleDebouncedSearch({
      query: "a",
      delayMs: 250,
      request,
      onReset,
      onStart: vi.fn(),
      onSuccess: vi.fn(),
      onError: vi.fn(),
    });
    await vi.advanceTimersByTimeAsync(300);

    expect(onReset).toHaveBeenCalledOnce();
    expect(request).not.toHaveBeenCalled();
  });

  // Burada debounce sonunda normalize edilmiş sorgu için yalnız bir istek oluştuğunu doğruluyorum.
  it("sends one request after the debounce period", async () => {
    vi.useFakeTimers();
    const request = vi.fn(async (query: string, signal: AbortSignal) => {
      void query;
      void signal;
      return { items: ["result"] };
    });
    const onSuccess = vi.fn();

    scheduleDebouncedSearch({
      query: "  inci   kolye ",
      delayMs: 250,
      request,
      onReset: vi.fn(),
      onStart: vi.fn(),
      onSuccess,
      onError: vi.fn(),
    });
    await vi.advanceTimersByTimeAsync(250);

    expect(request).toHaveBeenCalledOnce();
    expect(request.mock.calls[0]?.[0]).toBe("inci kolye");
    expect(onSuccess).toHaveBeenCalledWith({ items: ["result"] });
  });

  // Burada hızlı yazımda önceki debounce görevini iptal ederek ara sorgunun gönderilmediğini doğruluyorum.
  it("cancels the intermediate query during fast typing", async () => {
    vi.useFakeTimers();
    const request = vi.fn(async (query: string) => query);
    const firstCancel = scheduleDebouncedSearch({
      query: "in",
      delayMs: 250,
      request,
      onReset: vi.fn(),
      onStart: vi.fn(),
      onSuccess: vi.fn(),
      onError: vi.fn(),
    });
    firstCancel();
    scheduleDebouncedSearch({
      query: "inci",
      delayMs: 250,
      request,
      onReset: vi.fn(),
      onStart: vi.fn(),
      onSuccess: vi.fn(),
      onError: vi.fn(),
    });
    await vi.advanceTimersByTimeAsync(250);

    expect(request).toHaveBeenCalledOnce();
    expect(request.mock.calls[0]?.[0]).toBe("inci");
  });

  // Burada yeni sorgu veya modal kapanışı sırasında başlamış isteğin signal üzerinden gerçekten iptal edildiğini doğruluyorum.
  it("aborts an active request and ignores its late response", async () => {
    vi.useFakeTimers();
    let resolveRequest!: (value: string) => void;
    let receivedSignal: AbortSignal | undefined;
    const request = vi.fn((_: string, signal: AbortSignal) => {
      receivedSignal = signal;
      return new Promise<string>((resolve) => { resolveRequest = resolve; });
    });
    const onSuccess = vi.fn();
    const cancel = scheduleDebouncedSearch({
      query: "inci",
      delayMs: 250,
      request,
      onReset: vi.fn(),
      onStart: vi.fn(),
      onSuccess,
      onError: vi.fn(),
    });
    await vi.advanceTimersByTimeAsync(250);
    cancel();
    resolveRequest("late");
    await Promise.resolve();

    expect(receivedSignal?.aborted).toBe(true);
    expect(onSuccess).not.toHaveBeenCalled();
  });
});
