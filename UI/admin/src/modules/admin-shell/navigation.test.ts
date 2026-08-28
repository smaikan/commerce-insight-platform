import { describe, expect, it } from "vitest";
import { getCurrentNavigationHref, navigationSections, navigationStatusLabel } from "./navigation";

describe("admin navigation", () => {
  it("selects only the most specific accounting route", () => {
    expect(getCurrentNavigationHref("/accounting/current-accounts/a/edit")).toBe("/accounting/current-accounts");
    expect(getCurrentNavigationHref("/accounting/purchase-invoices/a/edit")).toBe("/accounting/purchase-invoices");
    expect(getCurrentNavigationHref("/accounting/sales-orders/a/edit")).toBe("/accounting/sales-orders");
    expect(getCurrentNavigationHref("/accounting/sales-invoices/a/edit")).toBe("/accounting/sales-invoices");
    expect(getCurrentNavigationHref("/accounting/payments/a")).toBe("/accounting/payments");
    expect(getCurrentNavigationHref("/accounting/treasury/bank/a")).toBe("/accounting/treasury");
    expect(getCurrentNavigationHref("/accounting/expenses")).toBe("/accounting/expenses");
    expect(getCurrentNavigationHref("/accounting/costing")).toBe("/accounting/costing");
    expect(getCurrentNavigationHref("/accounting/reports/sales")).toBe("/accounting/reports");
    expect(getCurrentNavigationHref("/accounting")).toBe("/accounting");
    expect(getCurrentNavigationHref("/unknown")).toBeUndefined();
  });

  // Burada kullanıma açılmış operasyon rotalarının navigasyonda etkin kaldığını doğruluyorum.
  it("keeps implemented operation routes enabled", () => {
    const enabledItems = navigationSections
      .flatMap((section) => section.items)
      .filter((item) => item.href);

    expect(enabledItems).toEqual([
      { label: "Genel Bakış", href: "/dashboard", status: "available" },
      { label: "Siparişler", href: "/orders", status: "available", workQueueKey: "orders" },
      { label: "İletişim Mesajları", href: "/contact-messages", status: "available", workQueueKey: "contactMessages" },
      { label: "Müşteriler", href: "/customers", status: "available" },
      { label: "İndirimler", href: "/coupons", status: "available" },
      { label: "Ürünler", href: "/products", status: "available" },
      { label: "Koleksiyonlar", href: "/collections", status: "available" },
      { label: "Markalar", href: "/brands", status: "available" },
      { label: "Stok İşlemleri", href: "/inventory/stock-movements", status: "available" },
      { label: "Bannerlar", href: "/banners", status: "available" },
      { label: "Google Analytics", href: "/marketing/google-analytics", status: "in-development" },
      { label: "Meta Reklam Yönetimi", href: "/marketing/meta-ads", status: "in-development" },
      { label: "Genel Bakış", href: "/accounting", status: "available" },
      { label: "Cari Hesaplar", href: "/accounting/current-accounts", status: "available" },
      { label: "Alış Faturaları", href: "/accounting/purchase-invoices", status: "available" },
      { label: "Muhasebe Satışları", href: "/accounting/sales-orders", status: "available" },
      { label: "Satış Faturaları", href: "/accounting/sales-invoices", status: "available" },
      { label: "Ödemeler ve Tahsilatlar", href: "/accounting/payments", status: "available" },
      { label: "Kasa ve Banka", href: "/accounting/treasury", status: "available" },
      { label: "Giderler", href: "/accounting/expenses", status: "available" },
      { label: "FIFO Maliyet", href: "/accounting/costing", status: "available" },
      { label: "Raporlar", href: "/accounting/reports", status: "available" },
      { label: "Yöneticiler", href: "/managers", status: "available" },
      { label: "Ayarlar", href: "/settings", status: "available" },
    ]);
  });

  // Burada vitrin banner yönetiminin katalog ve stok grubundan hemen sonra bağımsız bir bölümde yer aldığını doğruluyorum.
  it("places banners in the storefront section after catalog operations", () => {
    const catalogIndex = navigationSections.findIndex((section) => section.label === "Katalog ve Stok");
    expect(navigationSections[catalogIndex + 1]).toEqual({
      label: "Vitrin",
      items: [{ label: "Bannerlar", href: "/banners", status: "available" }],
    });
  });

  // Burada kullanıcının mevcut pazarlama geliştirme rotalarını vitrin sonrasında koruyorum.
  it("places development marketing routes after storefront", () => {
    const storefrontIndex = navigationSections.findIndex((section) => section.label === "Vitrin");
    expect(navigationSections[storefrontIndex + 1]).toEqual({
      label: "Pazarlama",
      items: [
        { label: "Google Analytics", href: "/marketing/google-analytics", status: "in-development" },
        { label: "Meta Reklam Yönetimi", href: "/marketing/meta-ads", status: "in-development" },
      ],
    });
  });

  // Burada yalnız uzun ve henüz kullanıma açılmamış bölümlerin açılır tutulduğunu doğruluyorum.
  it("keeps primary operations visible and future groups collapsible", () => {
    expect(navigationSections.filter((section) => section.collapsible).map((section) => section.label)).toEqual([
      "Muhasebe",
      "Pazaryeri Entegrasyonları",
    ]);
    expect(navigationSections.find((section) => section.label === "Muhasebe")?.status).toBe("available");
    expect(navigationSections.find((section) => section.label === "Pazaryeri Entegrasyonları")?.status).toBe("future");
  });

  it("labels unavailable navigation items while preserving explicit development routes", () => {
    const unavailableItems = navigationSections
      .flatMap((section) => section.items)
      .filter((item) => item.status !== "available");

    expect(unavailableItems.filter((item) => item.href).map((item) => item.href)).toEqual([
      "/marketing/google-analytics",
      "/marketing/meta-ads",
    ]);
    expect(unavailableItems.some((item) => item.label === "Ürün Ekle")).toBe(false);
    expect(unavailableItems.filter((item) => item.status === "in-development").map((item) => item.label)).toEqual([
      "Google Analytics",
      "Meta Reklam Yönetimi",
    ]);
    expect(navigationStatusLabel("in-development")).toBe("Geliştirme aşamasında");
    expect(navigationStatusLabel("next")).toBe("Sırada");
    expect(navigationStatusLabel("planned")).toBe("Planlı");
    expect(navigationStatusLabel("future")).toBe("Yakında");
  });
});
