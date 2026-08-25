import { NextRequest } from "next/server";
import { describe, expect, it, vi } from "vitest";
import {
  ADMIN_ACCESS_COOKIE,
  ADMIN_REFRESH_COOKIE,
  ADMIN_RETURN_TO_HEADER,
  PROTECTED_ADMIN_PREFIXES,
} from "./lib/auth/constants";

vi.mock("@/lib/site-config", () => import("./lib/site-config"));
vi.mock("@/lib/auth/constants", () => import("./lib/auth/constants"));
vi.mock("@/lib/auth/policy", () => import("./lib/auth/policy"));

import { config, proxy } from "./proxy";

describe("admin proxy return target", () => {
  // Burada Next.js matcher listesinin güvenli dönüş politikasındaki tüm uygulanmış Admin kökleriyle aynı kaldığını doğruluyorum.
  it("keeps the static matcher aligned with every protected admin prefix", () => {
    expect(config.matcher).toEqual(PROTECTED_ADMIN_PREFIXES.map((prefix) => `${prefix}/:path*`));
  });

  // Burada access cookie varken gerçek route ve query'nin istemciden gelen sahte headerı ezerek layout'a taşındığını doğruluyorum.
  it("forwards the exact protected request target to server rendering", () => {
    const request = new NextRequest("https://admin.example/products?page=2&pageSize=20&sort=created-desc", {
      headers: {
        cookie: `${ADMIN_ACCESS_COOKIE}=access-token`,
        [ADMIN_RETURN_TO_HEADER]: "/products?page=99",
      },
    });

    const response = proxy(request);

    expect(response.headers.get(`x-middleware-request-${ADMIN_RETURN_TO_HEADER}`))
      .toBe("/products?page=2&pageSize=20&sort=created-desc");
  });

  // Burada access cookie yokken refresh yönlendirmesinin de aynı tam ürün listesi hedefini koruduğunu doğruluyorum.
  it("preserves product pagination in the refresh redirect", () => {
    const request = new NextRequest("https://admin.example/products?page=3&pageSize=50", {
      headers: { cookie: `${ADMIN_REFRESH_COOKIE}=refresh-token` },
    });

    const response = proxy(request);
    const redirectUrl = new URL(response.headers.get("location") as string);

    expect(redirectUrl.pathname).toBe("/api/auth/refresh");
    expect(redirectUrl.searchParams.get("returnTo")).toBe("/products?page=3&pageSize=50");
  });

  // Burada ürün dışındaki Admin modüllerinde de refresh dönüşünün sayfa ve filtre durumunu kaybetmediğini doğruluyorum.
  it("preserves accounting pagination and filters in the refresh redirect", () => {
    const request = new NextRequest("https://admin.example/accounting/payments?pageNumber=4&type=2", {
      headers: { cookie: `${ADMIN_REFRESH_COOKIE}=refresh-token` },
    });

    const response = proxy(request);
    const redirectUrl = new URL(response.headers.get("location") as string);

    expect(redirectUrl.pathname).toBe("/api/auth/refresh");
    expect(redirectUrl.searchParams.get("returnTo")).toBe("/accounting/payments?pageNumber=4&type=2");
  });
});
