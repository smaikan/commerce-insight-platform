export function financialTypeLabel(type: number): string {
  const labels: Record<number, string> = { 1: "Müşteri tahsilatı", 2: "Tedarikçi ödemesi", 10: "Kasa girişi", 11: "Kasa çıkışı", 20: "Banka transfer girişi", 21: "Banka transfer çıkışı", 30: "POS tahsilatı", 40: "Banka komisyonu", 41: "Pazaryeri komisyonu", 50: "İade", 60: "Ters kayıt girişi", 61: "Ters kayıt çıkışı" };
  return labels[type] ?? `Bilinmeyen (${type})`;
}
export function sourceTypeLabel(type: number): string { const labels: Record<number, string> = { 1: "Alış faturası", 2: "Satış faturası", 3: "Muhasebe satışı", 4: "Ödeme", 5: "Finans işlemi" }; return labels[type] ?? `Bilinmeyen (${type})`; }
export function formatMoney(value: number, currencyCode = "TRY"): string { return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currencyCode }).format(value); }
export function formatDate(value: string): string { return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(new Date(value)); }
export function isSafelyReversible(transaction: FinancialTransactionLike): boolean {
  return transaction.sourceType === 5 && transaction.reversesTransactionId == null && ![20, 21, 60, 61].includes(transaction.type);
}
type FinancialTransactionLike = { sourceType: number; reversesTransactionId?: string | null; type: number };
