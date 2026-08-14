import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

const { useActionStateMock } = vi.hoisted(() => ({ useActionStateMock: vi.fn() }));

// Burada Client Component sonucunu kullanıcı varlığını açıklamayan statik bir başarı durumu ile izole ediyorum.
vi.mock("react", async (importOriginal) => ({
  ...await importOriginal<typeof import("react")>(),
  useActionState: useActionStateMock,
}));
vi.mock("@/modules/auth/password-reset-actions", () => ({ forgotPasswordAction: vi.fn() }));

import { ForgotPasswordForm } from "@/modules/auth/components/forgot-password-form";

describe("forgot-password result", () => {
  // Burada başarılı isteğin kalıcı genel mesaj ve gerçek geri dönüş yolları sunduğunu doğruluyorum.
  it("renders a non-enumerating success result", () => {
    useActionStateMock.mockReturnValue([{
      status: "success",
      revision: 1,
      message: "Bu e-posta sistemde kayıtlıysa parola sıfırlama bağlantısı gönderildi.",
    }, vi.fn()]);

    const html = renderToStaticMarkup(<ForgotPasswordForm />);

    expect(html).toContain('role="status"');
    expect(html).toContain("sistemde kayıtlıysa");
    expect(html).not.toMatch(/kayıtlı değil|kullanıcı bulunamadı/i);
    expect(html).toContain('href="/login"');
    expect(html).toContain('href="/forgot-password"');
  });
});
