import { describe, expect, it, vi } from "vitest";

import type { Cart } from "@/modules/cart/types";

import {
  cartErrorMessage,
  clearCartStateForOwnerChange,
  getCartSnapshot,
  isConflictProblem,
  loadCart,
  mutateCart,
} from "./cart-api";

describe("cart problem ayrımı", () => {
  // Burada yalnız API'nin concurrency kodunu yeniden yükleme gerektiren çakışma olarak kabul ettiğimi doğruluyorum.
  it("concurrency conflict hatasını tanır", () => {
    const problem = { status: 409, title: "Concurrency conflict", code: "concurrency_conflict" };

    expect(isConflictProblem(problem)).toBe(true);
    expect(cartErrorMessage(problem)).toContain("başka bir işlemde güncellendi");
  });

  // Burada stok kaynaklı 409 cevabının kullanıcıya yanlış concurrency mesajı göstermediğini doğruluyorum.
  it("stok conflict hatasını concurrency olarak sınıflandırmaz", () => {
    const problem = { status: 409, title: "Conflict", code: "conflict" };

    expect(isConflictProblem(problem)).toBe(false);
    expect(cartErrorMessage(problem)).toContain("stok veya satış durumu değişti");
  });

  // Burada GET, ekleme, adet güncelleme, farklı ürün ekleme ve checkout öncesi okumada son CartDto'nun varyant değerini aynen koruduğumu doğruluyorum.
  it("keeps variantValue from every authoritative cart response", async () => {
    const responses = [
      cartFixture("Pudra", 1),
      cartFixture("Pudra", 1),
      cartFixture("Pudra", 2),
      cartFixture("Lacivert", 3),
      cartFixture("Lacivert", 3),
    ];
    const fetchMock = vi.fn(async () => new Response(JSON.stringify(responses.shift()), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));

    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    try {
      const hydrated = await loadCart(true);
      const added = await mutateCart("/api/cart/items", { method: "POST" });
      const updated = await mutateCart("/api/cart/items/item-1", { method: "PUT" });
      const secondProduct = await mutateCart("/api/cart/items", { method: "POST" });
      const checkoutRead = await loadCart(true);

      expect(hydrated.items[0]?.variantValue).toBe("Pudra");
      expect(added.items[0]?.variantValue).toBe("Pudra");
      expect(updated.items[0]?.variantValue).toBe("Pudra");
      expect(secondProduct.items[0]?.variantValue).toBe("Lacivert");
      expect(checkoutRead.items[0]?.variantValue).toBe("Lacivert");
      expect(getCartSnapshot()).toBe(checkoutRead);
      expect(fetchMock).toHaveBeenCalledTimes(5);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  // Burada header ve sayfa aynı anda zorunlu yenileme istediğinde tek GET paylaşarak merge sonrası tazeliği N+1 isteğe çevirmediğimi doğruluyorum.
  it("deduplicates concurrent authoritative cart refreshes", async () => {
    const responseCart = cartFixture("Pudra", 1);
    const fetchMock = vi.fn(async () => new Response(JSON.stringify(responseCart), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    try {
      const headerRefresh = loadCart(true);
      const pageRefresh = loadCart(true);

      expect(pageRefresh).toBe(headerRefresh);
      await Promise.all([headerRefresh, pageRefresh]);
      expect(fetchMock).toHaveBeenCalledTimes(1);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  // Burada login/logout owner değişiminde önceki sepet snapshot'ını yeni kullanıcıya taşımadan sonraki GET'i yeniden çalıştırıyorum.
  it("clears the previous owner snapshot before reloading the cart", async () => {
    const firstCart = cartFixture("Pudra", 1);
    const nextCart = cartFixture("Lacivert", 2);
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify(firstCart), { status: 200, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify(nextCart), { status: 200, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", { dispatchEvent: vi.fn() });
    vi.stubGlobal("CustomEvent", class<T> {
      constructor(public type: string, public init: CustomEventInit<T>) {}
    });

    try {
      await loadCart(true);
      expect(getCartSnapshot()?.totalQuantity).toBe(1);

      clearCartStateForOwnerChange();
      expect(getCartSnapshot()).toBeNull();

      await expect(loadCart()).resolves.toMatchObject({ totalQuantity: 2 });
      expect(fetchMock).toHaveBeenCalledTimes(2);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  // Burada refresh gerektiren 401 cevabında mutation'ı tekrar etmeden güvenli dönüş hedefiyle tek yenileme navigasyonu başlatıyorum.
  it("starts one controlled session refresh for an authenticated 401", async () => {
    clearCartStateForOwnerChange();
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({
      status: 401,
      title: "Oturum yenilenmeli",
      code: "session_refresh_required",
    }), { status: 401, headers: { "Content-Type": "application/problem+json" } }));
    const assignMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("window", {
      dispatchEvent: vi.fn(),
      location: { pathname: "/cart", search: "?source=header", assign: assignMock },
    });

    try {
      await expect(loadCart(true)).rejects.toMatchObject({ code: "session_refresh_required" });
      await expect(loadCart(true)).rejects.toMatchObject({ code: "session_refresh_required" });
      expect(assignMock).toHaveBeenCalledOnce();
      expect(assignMock).toHaveBeenCalledWith("/api/auth/refresh?returnTo=%2Fcart%3Fsource%3Dheader");
      expect(fetchMock).toHaveBeenCalledTimes(2);
    } finally {
      vi.unstubAllGlobals();
    }
  });
});

// Burada cart client regresyonunda generated CartDto alanlarını eksiksiz temsil eden küçük bir fixture üretiyorum.
function cartFixture(variantValue: string, totalQuantity: number): Cart {
  return {
    id: "6cf01506-270c-45a8-8d0c-e957a2ae873c",
    concurrencyToken: crypto.randomUUID(),
    items: [{
      id: "8d52d55c-1acd-4c54-a9a0-3354e9f0d263",
      productId: "P00001",
      productVariantId: "a71e05d8-d9ce-4351-88f2-1b52580ae39e",
      productTitle: "Test yüzüğü",
      variantName: "Renk",
      variantValue,
      sku: "SKU-TEST",
      quantity: totalQuantity,
      unitPrice: 1200,
      currentUnitPrice: 1200,
      totalPrice: 1200 * totalQuantity,
      availableStock: 8,
      isAvailable: true,
      priceChanged: false,
      createdAt: "2026-08-13T08:00:00Z",
    }],
    totalQuantity,
    subTotal: 1200 * totalQuantity,
    hasUnavailableItems: false,
    hasPriceChanges: false,
  };
}
