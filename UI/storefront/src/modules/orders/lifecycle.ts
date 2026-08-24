// Burada müşteri aksiyonlarının numeric OrderStatus sınırlarını tek yerde ve birbirinden ayrık tutuyorum.
const CUSTOMER_CANCELLATION_STATUSES = new Set([0, 1, 2, 3]);
const RETURN_CENTER_STATUSES = new Set([4, 5, 7, 8, 9]);
const RETURN_REQUEST_STATUSES = new Set([5, 7, 8, 9]);

// Burada kargoya verilmemiş Pending, Confirmed, Paid ve Preparing siparişleri ortak müşteri iptal aksiyonuna alıyorum.
export function canCustomerCancelOrder(status: number): boolean {
  return CUSTOMER_CANCELLATION_STATUSES.has(status);
}

// Burada kargoya verilen siparişten itibaren satış sonrası merkezini görünür kılıyor, iptal edilmiş siparişi bu akışa almıyorum.
export function canOpenOrderReturnCenter(status: number): boolean {
  return RETURN_CENTER_STATUSES.has(status);
}

// Burada gerçek talep formunu yalnız API'nin kabul ettiği teslim edilmiş veya aktif iade durumlarında açıyorum.
export function canCreateOrderReturnRequest(status: number): boolean {
  return RETURN_REQUEST_STATUSES.has(status);
}
