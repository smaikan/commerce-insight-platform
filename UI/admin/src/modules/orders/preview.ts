import type { Order, OrderListPreview, ReturnRequest } from "@/modules/orders/types";

// Burada tam sipariş DTO'sunu hızlı görünümün ihtiyaç duyduğu güvenli ve küçük alan kümesine indiriyorum.
export function toOrderListPreview(order: Order, returns: ReturnRequest[] | null = []): OrderListPreview {
  const availableReturns = returns ?? [];
  return {
    id: order.id,
    orderNumber: order.orderNumber,
    customer: order.customer
      ? {
          firstName: order.customer.firstName,
          lastName: order.customer.lastName,
          email: order.customer.email,
          phoneNumber: order.customer.phoneNumber,
        }
      : undefined,
    shippingAddress: order.shippingAddress
      ? {
          title: order.shippingAddress.title,
          firstName: order.shippingAddress.firstName,
          lastName: order.shippingAddress.lastName,
          phoneNumber: order.shippingAddress.phoneNumber,
          city: order.shippingAddress.city,
          district: order.shippingAddress.district,
          fullAddress: order.shippingAddress.fullAddress,
          postalCode: order.shippingAddress.postalCode,
        }
      : undefined,
    items: order.items.map((item) => ({
      id: item.id,
      productId: item.productId,
      productTitle: item.productTitle,
      variantSku: item.variantSku,
      quantity: item.quantity,
      totalPrice: item.totalPrice,
      returns: availableReturns.flatMap((returnRequest) => returnRequest.items
        .filter((returnItem) => returnItem.orderItemId === item.id)
        .map((returnItem) => ({
          id: returnRequest.id,
          returnNumber: returnRequest.returnNumber,
          type: returnRequest.type,
          status: returnRequest.status,
          quantity: returnItem.quantity,
        }))),
    })),
    grandTotal: order.grandTotal,
    returnsUnavailable: returns === null,
  };
}
