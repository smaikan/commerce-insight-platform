import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
vi.mock("@/modules/store-settings/api", () => ({
  getPublicStoreSettings: vi.fn().mockResolvedValue({
    displayName: "ELEVEN ACCESSORY",
    supportEmail: "info@eleven.com",
    supportPhone: "0536 256 78 45",
    contactAddress: "Altıyol Meydanı, Söğütlüçeşme Cad., 34714 Kadıköy/İstanbul",
  }),
}));

import MembershipAgreementPage from "@/app/(store)/membership-agreement/page";
import MembershipPrivacyNoticePage from "@/app/(store)/membership-privacy-notice/page";
import PrivacyPolicyPage from "@/app/(store)/privacy-policy/page";

describe("membership legal pages", () => {
  it("renders the membership agreement with store settings", async () => {
    const page = await MembershipAgreementPage();
    const html = renderToStaticMarkup(page);

    expect(html).toContain("Üyelik Sözleşmesi");
    expect(html).toContain("Üyeliğin kurulması");
    expect(html).toContain("ELEVEN ACCESSORY");
    expect(html).toContain('href="/membership-privacy-notice"');
    expect(html.match(/<h1/g)).toHaveLength(1);
  });

  it("renders the membership privacy notice separately from consent", async () => {
    const page = await MembershipPrivacyNoticePage();
    const html = renderToStaticMarkup(page);

    expect(html).toContain("Üyelik KVKK Aydınlatma Metni");
    expect(html).toContain("Bu metin bir açık rıza veya pazarlama izni değildir.");
    expect(html).toContain("info@eleven.com");
    expect(html).toContain('href="/membership-agreement"');
    expect(html.match(/<h1/g)).toHaveLength(1);
  });

  it("renders the privacy policy page with dynamic store settings", async () => {
    const page = await PrivacyPolicyPage();
    const html = renderToStaticMarkup(page);

    expect(html).toContain("KVKK ve Gizlilik Politikası");
    expect(html).toContain("ELEVEN ACCESSORY");
    expect(html).toContain("info@eleven.com");
    expect(html).toContain("0536 256 78 45");
    expect(html).toContain("Siparişe özel ön bilgilendirme formunda gösterilecektir.");
    expect(html.match(/<h1/g)).toHaveLength(1);
  });
});
