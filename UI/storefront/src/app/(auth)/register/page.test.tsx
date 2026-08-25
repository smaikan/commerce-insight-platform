import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/components/storefront/site-header", () => ({
  SiteHeader: () => <header data-testid="storefront-navbar">Mağaza navigasyonu</header>,
}));
vi.mock("@/modules/auth/components/auth-shell", () => ({
  AuthShell: ({ children }: { children: React.ReactNode }) => <main id="main-content">{children}</main>,
}));
vi.mock("@/modules/auth/components/register-form", () => ({
  RegisterForm: () => <form aria-label="Kayıt formu" />,
}));

import RegisterPage from "./page";

describe("register page", () => {
  it("renders the storefront navbar above the registration form", () => {
    const html = renderToStaticMarkup(<RegisterPage />);

    expect(html).toContain('data-testid="storefront-navbar"');
    expect(html).toContain('aria-label="Kayıt formu"');
    expect(html.indexOf("Mağaza navigasyonu")).toBeLessThan(html.indexOf('id="main-content"'));
  });
});
