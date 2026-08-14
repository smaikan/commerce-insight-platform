import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { AuthField, GoogleDevelopmentButton, PasswordField } from "./auth-controls";

describe("auth controls", () => {
  // Burada geçersiz alanın label, aria-invalid ve açıklayıcı hata metniyle erişilebilir biçimde bağlandığını doğruluyorum.
  it("connects field errors to their input", () => {
    const html = renderToStaticMarkup(<AuthField id="email" name="email" label="E-posta" autoComplete="email" error="Geçersiz" />);
    expect(html).toContain('for="email"');
    expect(html).toContain('aria-invalid="true"');
    expect(html).toContain('aria-describedby="email-error"');
    expect(html).toContain('id="email-error"');
  });

  // Burada şifre alanının ilk HTML'de gizli, autocomplete uyumlu ve görünürlük kontrolüyle etiketli olduğunu doğruluyorum.
  it("renders a secure password field", () => {
    const html = renderToStaticMarkup(<PasswordField id="password" name="password" label="Şifre" autoComplete="current-password" />);
    expect(html).toContain('type="password"');
    expect(html).toContain('autoComplete="current-password"');
    expect(html).toContain('aria-pressed="false"');
  });

  // Burada Google entegrasyonunun SDK yüklemeden devre dışı ve geliştirme durumu açık olacak şekilde sunulduğunu doğruluyorum.
  it("keeps Google authentication disabled during development", () => {
    const html = renderToStaticMarkup(<GoogleDevelopmentButton />);
    expect(html).toContain("Google ile devam et");
    expect(html).toContain("Geliştirme aşamasında");
    expect(html).toContain("disabled");
  });
});
