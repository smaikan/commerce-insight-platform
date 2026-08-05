import type { components } from "@/generated/api";

// Burada sipariş wire modellerini generated OpenAPI şemalarından okunabilir feature adlarına bağlıyorum.
export type Order = components["schemas"]["OrderDto"];
export type OrderSummary = components["schemas"]["OrderSummaryDto"];
export type OrderStatus = components["schemas"]["OrderStatus"];
export type OrderPayment = components["schemas"]["ECommerce.Application.Orders.Dtos.PaymentDto"];
export type PaymentStatus = components["schemas"]["ECommerce.Domain.Enums.PaymentStatus"];
export type PaymentProvider = components["schemas"]["PaymentProvider"];

export type OrderPage = components["schemas"]["OrderSummaryDtoPagedResult"];

// Burada açılır liste özetinde tarayıcıya yalnız gerekli müşteri, teslimat ve ürün alanlarını taşıyorum.
export type OrderListPreview = {
  id: string;
  orderNumber: string;
  customer?: {
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
  };
  shippingAddress?: {
    title: string;
    firstName: string;
    lastName: string;
    phoneNumber: string;
    city: string;
    district: string;
    fullAddress: string;
    postalCode?: string | null;
  };
  items: Array<{
    id: string;
    productId: string;
    productTitle: string;
    variantSku: string;
    quantity: number;
    totalPrice: number;
  }>;
  grandTotal: number;
};

// Burada BFF hata cevabında kullanıcıya yalnız güvenli mesajı ve destek takip kodunu açıyorum.
export type OrderPreviewError = {
  message: string;
  traceId?: string;
};

// Burada URL filtresi ile API'ye gönderilecek UTC tarih sınırlarını aynı liste sorgusunda taşıyorum.
export type OrderListQuery = {
  pageNumber: number;
  pageSize: number;
  status?: OrderStatus;
  createdFrom?: string;
  createdTo?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
  dateError?: string;
};
