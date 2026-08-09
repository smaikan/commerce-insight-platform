// Burada parasal dashboard metriğini Türk lirası olarak okunaklı ve tutarlı biçimde sunuyorum.
export function formatDashboardCurrency(value: number): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: "TRY",
    maximumFractionDigits: 2,
  }).format(value);
}

// Burada API'nin UTC üretim zamanını yönetim paneli için Türkiye saatine çeviriyorum.
export function formatDashboardGeneratedAt(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Güncelleme zamanı alınamadı";

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Europe/Istanbul",
  }).format(date);
}
