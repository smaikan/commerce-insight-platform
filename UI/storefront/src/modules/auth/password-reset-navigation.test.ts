import { describe, expect, it, vi } from "vitest";

import { redirectAfterPasswordReset } from "@/modules/auth/password-reset-navigation";

describe("password reset navigation", () => {
  // Burada başarı sonrasında istemci belleğini koruyan iç yönlendirme yerine tam login yüklemesinin istendiğini doğruluyorum.
  it("performs a full-document login redirect", () => {
    const replace = vi.fn();

    redirectAfterPasswordReset({ replace });

    expect(replace).toHaveBeenCalledWith("/login?passwordReset=1");
  });
});
