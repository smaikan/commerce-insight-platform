import "server-only";

import { apiGet } from "@/lib/api/client";
import type {
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
