import "server-only";

import { apiGet } from "@/lib/api/client";
import { authenticatedApiRequest } from "@/lib/api/authenticated-client";
import type {
  CheckoutOrder,
  MemberCheckoutRequest,
  ShippingMethod,
  ShippingMethodPage,
} from "@/modules/checkout/types";

// Burada checkout için yalnız aktif kargo seçeneklerini kişisel veri cache'ine karıştırmadan güncel API sözleşmesinden okuyorum.
export async function getActiveShippingMethods(): Promise<ShippingMethod[]> {
  const page = await apiGet<ShippingMethodPage>(
    "/api/shipping-methods/active?pageNumber=1&pageSize=100",
    { revalidate: 0 },
  );
  return page.items.filter((method) => method.isActive);
}

// Burada üye sepetini sahiplik denetimli adres ve cart tokenıyla authoritative siparişe dönüştürüyorum.
export function createMemberOrder(payload: MemberCheckoutRequest): Promise<CheckoutOrder> {
  return authenticatedApiRequest<CheckoutOrder>("/api/orders", { method: "POST", body: payload });
}
