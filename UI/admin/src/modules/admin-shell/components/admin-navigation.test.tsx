import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ usePathname: () => "/dashboard" }));

import { AdminNavigation } from "./admin-navigation";

describe("AdminNavigation work queue badges", () => {
  // Burada sipariş ve mesaj sayaçlarının anlamlı erişilebilir adlarla, dekoratif rozetleri tekrar okutmayacak biçimde çizildiğini doğruluyorum.
  it("renders accessible operational counts", () => {
    const html = renderToStaticMarkup(
      <AdminNavigation
        initialSummary={{
          ordersAwaitingProcessingCount: 3,
          newContactMessageCount: 1,
          generatedAtUtc: "2026-08-27T10:00:00Z",
        }}
        initialUnavailable={false}
        mode="desktop"
      />,
    );

    expect(html).toContain('aria-label="Siparişler, 3 işlem bekleyen sipariş"');
    expect(html).toContain('aria-label="İletişim Mesajları, 1 yeni iletişim mesajı"');
    expect(html).toContain('title="3 kayıt"');
    expect(html).toContain('title="1 kayıt"');
    expect(html.match(/aria-hidden="true"/g)?.length).toBeGreaterThanOrEqual(2);
  });

  // Burada sıfır sayaçların yanıltıcı rozet veya gereksiz erişilebilir açıklama üretmediğini doğruluyorum.
  it("omits empty badges", () => {
    const html = renderToStaticMarkup(
      <AdminNavigation
        initialSummary={{
          ordersAwaitingProcessingCount: 0,
          newContactMessageCount: 0,
          generatedAtUtc: "2026-08-27T10:00:00Z",
        }}
        initialUnavailable={false}
        mode="desktop"
      />,
    );

    expect(html).not.toContain("işlem bekleyen sipariş");
    expect(html).not.toContain("yeni iletişim mesajı");
    expect(html).not.toContain("kayıt");
  });
});
