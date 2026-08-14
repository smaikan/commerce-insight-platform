import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { authCookieNames } from "./cookies";

describe("storefront auth cookie names", () => {
  // Burada production auth çerezlerinin __Host- korumasını ve admin oturumundan ayrı Storefront adlarını taşıdığını doğruluyorum.
  it("uses host-bound names in production", () => {
    expect(authCookieNames("production")).toEqual({
      access: "__Host-ecommerce_storefront_access",
      refresh: "__Host-ecommerce_storefront_refresh",
    });
  });

  // Burada localhost geliştirmesinde Secure zorunluluğuna takılmayacak öneksiz fakat ayrı isimleri doğruluyorum.
  it("uses localhost-compatible names outside production", () => {
    expect(authCookieNames("development")).toEqual({
      access: "ecommerce_storefront_access",
      refresh: "ecommerce_storefront_refresh",
    });
  });
});
