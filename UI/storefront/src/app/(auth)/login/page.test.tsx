import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/components/storefront/site-header", () => ({
  SiteHeader: () => <header data-testid="storefront-navbar">Mağaza navigasyonu</header>,
}));
vi.mock("@/modules/auth/components/auth-shell", () => ({
  AuthShell: ({ children }: { children: React.ReactNode }) => <main id="main-content">{children}</main>,
}));
vi.mock("@/modules/auth/components/login-form", () => ({
  LoginForm: () => <form aria-label="Giriş formu" />,
}));

import LoginPage from "./page";

describe("login page", () => {
  // Burada ortak Storefront navbar'ının login ana içeriğinden önce render edildiğini doğruluyorum.
  it("renders the storefront navbar above the login form", async () => {
    const page = await LoginPage({ searchParams: Promise.resolve({}) });
    const html = renderToStaticMarkup(page);

    expect(html).toContain('data-testid="storefront-navbar"');
    expect(html).toContain('aria-label="Giriş formu"');
    expect(html.indexOf("Mağaza navigasyonu")).toBeLessThan(html.indexOf('id="main-content"'));
  });
});
