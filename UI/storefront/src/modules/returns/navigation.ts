// Burada magic-link ile doğrulanan misafir siparişini tokenı URL'ye taşımadan guest sahiplik modundaki confirmation ekranına yönlendiriyorum.
export function guestOrderConfirmationHref(orderId: string): string {
  return `/checkout/confirmation/${encodeURIComponent(orderId)}?access=guest`;
}
