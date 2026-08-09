import type { components } from "@/generated/api";

// Burada kupon yönetiminin tüm API şekillerini üretilmiş OpenAPI sözleşmesinden alıyorum.
export type Coupon = components["schemas"]["CouponDto"];
export type CouponPage = components["schemas"]["CouponDtoPagedResult"];
export type CouponRequest = components["schemas"]["CouponRequest"];
export type CouponDiscountType = components["schemas"]["CouponDiscountType"];

// Burada URL'den çözülen, yalnız sözleşmede bulunan kupon listeleme filtrelerini taşıyorum.
export type CouponListQuery = {
  pageNumber: number;
  pageSize: number;
  isActive?: boolean;
};

// Burada formdaki güvenli hata ve başarılı işlem bilgisini istemciye taşıyorum.
export type CouponActionState = {
  status: "idle" | "error";
  message?: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
};

export const initialCouponActionState: CouponActionState = { status: "idle" };
