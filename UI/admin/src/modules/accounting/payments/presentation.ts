export function paymentTypeLabel(type: number): string { return type === 1 ? "Müşteri tahsilatı" : type === 2 ? "Tedarikçi ödemesi" : `Bilinmeyen (${type})`; }
export function paymentStatusLabel(status: number): string { return status === 1 ? "Tamamlandı" : status === 2 ? "İptal edildi" : status === 3 ? "Terslendi" : `Bilinmeyen (${status})`; }
export function paymentStatusClass(status: number): string { return status === 1 ? "border-emerald-300 bg-emerald-50 text-emerald-800" : status === 2 || status === 3 ? "border-slate-300 bg-slate-100 text-slate-700" : "border-amber-300 bg-amber-50 text-amber-900"; }
export function formatMoney(value: number, currencyCode = "TRY"): string { return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currencyCode }).format(value); }
export function formatAccountingDate(value: string): string { return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(new Date(value)); }
