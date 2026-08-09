// Burada kargo ücretlerini panelin Türkçe para gösterimine dönüştürüyorum.
export function formatTry(value: number): string {
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value);
}

// Burada vergi oranını yüzde işaretiyle fakat sözleşmedeki değeri değiştirmeden gösteriyorum.
export function formatRate(value: number): string {
  return new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(value) + "%";
}

// Burada ayar değişiklik tarihini ortak ve kısa bir Türkçe biçimde gösteriyorum.
export function formatSettingsDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" }).format(date);
}
