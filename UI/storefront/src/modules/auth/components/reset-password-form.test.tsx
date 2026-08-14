import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

const reactMocks = vi.hoisted(() => ({
  useActionState: vi.fn(),
  useEffect: vi.fn(),
  useRef: vi.fn(),
  useState: vi.fn(),
}));

// Burada fragment okumasını çalıştırmadan hazır ve geçersiz ekran durumlarını güvenli statik fixturelarla izole ediyorum.
vi.mock("react", async (importOriginal) => ({
  ...await importOriginal<typeof import("react")>(),
  useActionState: reactMocks.useActionState,
  useEffect: reactMocks.useEffect,
  useRef: reactMocks.useRef,
  useState: reactMocks.useState,
}));
vi.mock("@/modules/auth/password-reset-actions", () => ({ resetPasswordAction: vi.fn() }));

import { ResetPasswordForm } from "@/modules/auth/components/reset-password-form";

describe("reset-password form", () => {
  // Burada hazır formun iki parola alanını sunduğunu ve tokenı görünür HTML'e taşımadığını doğruluyorum.
  it("renders accessible password fields without exposing the token", () => {
    reactMocks.useRef.mockReturnValue({ current: true });
    reactMocks.useState.mockImplementation((initial: unknown) => typeof initial === "boolean"
      ? [initial, vi.fn()]
      : [{ status: "ready", token: "fixture-sensitive-token" }, vi.fn()]);
    reactMocks.useActionState.mockReturnValue([{ status: "idle", revision: 0 }, vi.fn()]);

    const html = renderToStaticMarkup(<ResetPasswordForm />);

    expect(html).toContain('name="newPassword"');
    expect(html).toContain('name="confirmPassword"');
    expect(html).toContain('autoComplete="new-password"');
    expect(html).toContain('type="password"');
    expect(html).not.toContain("fixture-sensitive-token");
    expect(html).not.toContain('name="token"');
  });

  // Burada token bulunmayan ekranın API formu yerine tek bir yeni bağlantı eylemi sunduğunu doğruluyorum.
  it("renders a safe invalid-link recovery state", () => {
    reactMocks.useRef.mockReturnValue({ current: true });
    reactMocks.useState.mockImplementation(() => [{ status: "invalid" }, vi.fn()]);

    const html = renderToStaticMarkup(<ResetPasswordForm />);

    expect(html).toContain('role="alert"');
    expect(html).toContain('href="/forgot-password"');
    expect(html).not.toContain('name="newPassword"');
  });
});
