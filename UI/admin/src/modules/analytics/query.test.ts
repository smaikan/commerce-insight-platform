import { describe, expect, it } from "vitest";
import { buildAnalyticsPeriodHref, getAnalyticsDateRange, parseAnalyticsPeriod } from "./query";

describe("analytics query", () => {
  // Burada geçersiz dönemlerin API sınırını aşmayan varsayılan 30 güne döndüğünü doğruluyorum.
  it("uses 30 days for an unsupported period", () => {
    expect(parseAnalyticsPeriod({ analyticsPeriod: "365" })).toBe(30);
  });

  // Burada 7 günlük dönemin UTC gününde bitip iki ucu da içerdiğini doğruluyorum.
  it("builds an inclusive UTC date range", () => {
    expect(getAnalyticsDateRange(7, new Date("2026-08-07T23:50:00+03:00"))).toEqual({
      period: 7,
      from: "2026-08-01",
      to: "2026-08-07",
    });
  });

  // Burada dönem bağlantısının aynı sayfadaki güvenli bağlam parametrelerini koruduğunu doğruluyorum.
  it("preserves other search parameters when changing period", () => {
    expect(buildAnalyticsPeriodHref("/products/P00001", { saved: "1", analyticsPeriod: "7" }, 90)).toBe(
      "/products/P00001?saved=1&analyticsPeriod=90",
    );
  });
});
