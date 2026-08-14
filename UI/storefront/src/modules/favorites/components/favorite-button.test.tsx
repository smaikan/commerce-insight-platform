import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

import { canToggleFavorite, FavoriteButton } from "./favorite-button";

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }) }));
vi.mock("@/modules/auth/components/header-session", () => ({ useHeaderSession: () => "authenticated" }));

describe("favorite button presentation", () => {
  // Burada guest favori tıklamasının login yönlendirmesi yerine doğrudan mutation akışına açık olduğunu doğruluyorum.
  it("allows guests to toggle favorites", () => {
    expect(canToggleFavorite("guest")).toBe(true);
    expect(canToggleFavorite("authenticated")).toBe(true);
    expect(canToggleFavorite("loading")).toBe(false);
  });

  // Burada kart kontrolünün 44px erişilebilir hedef içinde daha küçük ve hafif bir görsel kalp yüzeyi kullandığını doğruluyorum.
  it("keeps a compact visual heart inside the card hit target", () => {
    const html = renderToStaticMarkup(<FavoriteButton productId="P00001" productTitle="İnci Kolye" />);

    expect(html).toContain("size-11");
    expect(html).toContain("size-8");
    expect(html).toContain("backdrop-blur-md");
    expect(html).toContain('aria-label="İnci Kolye ürününü favorilere ekle"');
  });

  // Burada ürün başlığının yanındaki detay kontrolünün metin veya kaba çerçeve taşımayan yalnız ikonlu buton olduğunu doğruluyorum.
  it("renders the detail favorite control as an icon-only button", () => {
    const html = renderToStaticMarkup(<FavoriteButton productId="P00001" productTitle="İnci Kolye" variant="detail" />);

    expect(html).toContain("size-[1.3rem]");
    expect(html).toContain("shrink-0");
    expect(html).not.toContain("Güncelleniyor");
    expect(html).not.toContain("border px-4");
  });
});
