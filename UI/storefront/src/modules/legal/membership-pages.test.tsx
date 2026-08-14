import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import MembershipAgreementPage from "@/app/(store)/membership-agreement/page";
import MembershipPrivacyNoticePage from "@/app/(store)/membership-privacy-notice/page";

describe("membership legal pages", () => {
  // Burada üyelik sözleşmesinin hesap, sipariş ve kişisel veri sınırlarını ilk HTML içinde ayrı bölümlerle sunduğunu doğruluyorum.
  it("renders the membership agreement", () => {
    const html = renderToStaticMarkup(<MembershipAgreementPage />);

    expect(html).toContain("Üyelik Sözleşmesi");
    expect(html).toContain("Üyeliğin kurulması");
    expect(html).toContain("Şifre ve hesap güvenliği");
    expect(html).toContain('href="/membership-privacy-notice"');
    expect(html.match(/<h1/g)).toHaveLength(1);
  });

  // Burada üyeliğe özel KVKK metninin aydınlatmayı açık rıza veya pazarlama izni gibi sunmadığını doğruluyorum.
  it("renders the membership privacy notice separately from consent", () => {
    const html = renderToStaticMarkup(<MembershipPrivacyNoticePage />);

    expect(html).toContain("Üyelik KVKK Aydınlatma Metni");
    expect(html).toContain("Bu metin bir açık rıza veya pazarlama izni değildir.");
    expect(html).toContain("Hukuki sebepler");
    expect(html).toContain('href="/membership-agreement"');
    expect(html.match(/<h1/g)).toHaveLength(1);
  });
});
