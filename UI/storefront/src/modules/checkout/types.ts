import type { components } from "@/generated/api";

export type GuestCheckoutRequest = components["schemas"]["GuestCheckoutRequest"];
export type GuestAddressRequest = components["schemas"]["GuestAddressRequest"];
export type ShippingMethod = components["schemas"]["ShippingMethodDto"];
export type ShippingMethodPage = components["schemas"]["ShippingMethodDtoPagedResult"];
export type GuestOrder = components["schemas"]["OrderDto"];
export type CheckoutOrder = components["schemas"]["OrderDto"];
export type CheckoutFormSession = components["schemas"]["CheckoutFormSessionDto"];
export type MemberCheckoutRequest = components["schemas"]["CreateOrderRequest"];
export type CheckoutAddress = components["schemas"]["AddressDto"];

export type CheckoutProblem = {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};

export type MemberCheckoutActionResult =
  | { ok: true; order: CheckoutOrder }
  | { ok: false; problem: CheckoutProblem };
