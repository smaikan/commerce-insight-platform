import { siteConfig } from "@/lib/site-config";

const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: siteConfig.currency,
  minimumFractionDigits: 2,
});

// Burada API'nin otoriter fiyatını merkezi mağaza para birimiyle yalnız sunum için biçimlendiriyorum.
export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}
