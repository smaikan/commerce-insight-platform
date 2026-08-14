import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

const { useActionStateMock } = vi.hoisted(() => ({ useActionStateMock: vi.fn() }));

// Burada Client Component form durumunu statik ve kişisel veri içermeyen bir fixture ile izole ediyorum.
vi.mock("react", async (importOriginal) => ({
  ...await importOriginal<typeof import("react")>(),
  useActionState: useActionStateMock,
}));
vi.mock("@/modules/auth/actions", () => ({ registerAction: vi.fn() }));

import { RegisterForm } from "./register-form";

describe("register form legal consent", () => {
  // Burada kayıt formunun zorunlu checkbox'ı iki ayrı yasal metin bağlantısıyla ve açık rıza iddiası olmadan sunduğunu doğruluyorum.
  it("renders required membership and privacy notice acknowledgement", () => {
    useActionStateMock.mockReturnValue([{ status: "idle", revision: 0 }, vi.fn()]);

    const html = renderToStaticMarkup(<RegisterForm />);

    expect(html).toContain('name="legalConsent"');
    expect(html).toContain('type="checkbox"');
    expect(html).toContain("required");
    expect(html).toContain('href="/membership-agreement"');
    expect(html).toContain('href="/membership-privacy-notice"');
    expect(html).toContain("Üyelik Sözleşmesi");
    expect(html).toContain("KVKK Aydınlatma Metni");
    expect(html).not.toContain("açık rıza veriyorum");
  });
});
