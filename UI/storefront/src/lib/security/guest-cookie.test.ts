import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { appendAllowedGuestSetCookies, guestCookieHeader } from "./guest-cookie";

const token = "A".repeat(64);
const originalNodeEnv = process.env.NODE_ENV;
const mutableEnv = process.env as Record<string, string | undefined>;

afterEach(() => {
  if (originalNodeEnv === undefined) delete mutableEnv.NODE_ENV;
  else mutableEnv.NODE_ENV = originalNodeEnv;
});

describe("guest cookie boundary", () => {
  // Burada yalnız allowlist içindeki canonical token'ın upstream cookie başlığına girdiğini doğruluyorum.
  it("forwards only allowlisted canonical guest cookies", () => {
    expect(guestCookieHeader(
      `session=secret; ecommerce_guest_cart=${token}; ecommerce_guest_orders=bad`,
      ["ecommerce_guest_cart", "ecommerce_guest_orders"],
    )).toBe(`ecommerce_guest_cart=${token}`);
  });

  // Burada bilinmeyen cookie, bozuk token ve upstream Domain/Path niteliklerinin browser cevabına sızmadığını doğruluyorum.
  it("rebuilds allowed set-cookie values with storefront-owned attributes", () => {
    mutableEnv.NODE_ENV = "production";
    const source = new Headers();
    source.append("Set-Cookie", `ecommerce_guest_cart=${token}; Domain=api.example.com; Path=/api/cart; SameSite=None`);
    source.append("Set-Cookie", `unknown_cookie=${token}; HttpOnly`);
    source.append("Set-Cookie", "ecommerce_guest_orders=invalid; HttpOnly");
    const target = new Headers();

    appendAllowedGuestSetCookies(source, target, ["ecommerce_guest_cart", "ecommerce_guest_orders"]);

    const values = readSetCookies(target);
    expect(values).toHaveLength(1);
    expect(values[0]).toBe(`ecommerce_guest_cart=${token}; Path=/; HttpOnly; SameSite=Lax; Secure`);
  });

  // Burada checkout sonrası canonical guest cookie silme talebinin güvenli niteliklerle korunabildiğini doğruluyorum.
  it("allows deletion of an allowlisted guest cookie", () => {
    const source = new Headers({
      "Set-Cookie": "ecommerce_guest_cart=; Domain=api.example.com; Path=/api/cart; Max-Age=0",
    });
    const target = new Headers();

    appendAllowedGuestSetCookies(source, target, ["ecommerce_guest_cart"]);

    expect(target.get("set-cookie")).toBe("ecommerce_guest_cart=; Path=/; HttpOnly; SameSite=Lax; Max-Age=0");
  });
});

function readSetCookies(headers: Headers): string[] {
  const getSetCookie = (headers as Headers & { getSetCookie?: () => string[] }).getSetCookie;
  return getSetCookie ? getSetCookie.call(headers) : [headers.get("set-cookie")].filter((value): value is string => Boolean(value));
}
