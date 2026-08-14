import { describe, expect, it } from "vitest";

import { orderItemHref, safeTrackingUrl } from "@/modules/account/presentation";

describe("account order presentation", () => {
  // Burada API slug'ındaki özel karakterlerin tek güvenli ürün segmentine encode edildiğini doğruluyorum.
  it("encodes the order item product slug", () => {
    expect(orderItemHref("özel yüzük/seri 1")).toBe("/products/%C3%B6zel%20y%C3%BCz%C3%BCk%2Fseri%201");
  });

  // Burada eski snapshot'ta ürün URL'si yoksa tıklanabilir hedef üretmediğimi doğruluyorum.
  it("does not create a product link without a snapshot slug", () => {
    expect(orderItemHref(null)).toBeNull();
    expect(orderItemHref("   ")).toBeNull();
  });

  // Burada kargo linkinde yalnız mutlak HTTP ve HTTPS protokollerine izin verdiğimi doğruluyorum.
  it("accepts safe tracking URLs and rejects unsafe schemes", () => {
    expect(safeTrackingUrl("https://cargo.example/track/123")).toBe("https://cargo.example/track/123");
    expect(safeTrackingUrl("http://cargo.example/track/123")).toBe("http://cargo.example/track/123");
    expect(safeTrackingUrl("javascript:alert(1)")).toBeNull();
    expect(safeTrackingUrl("//cargo.example/track/123")).toBeNull();
    expect(safeTrackingUrl("/track/123")).toBeNull();
  });
});
