import type { components } from "@/generated/api";

// Burada ayarlar modülünün wire tiplerini üretilmiş OpenAPI sözleşmesine bağlıyorum.
export type ShippingMethod = components["schemas"]["ShippingMethodDto"];
export type ShippingMethodPage = components["schemas"]["ShippingMethodDtoPagedResult"];
export type CreateShippingMethodRequest = components["schemas"]["CreateShippingMethodRequest"];
export type UpdateShippingMethodRequest = components["schemas"]["UpdateShippingMethodRequest"];
export type TaxRate = components["schemas"]["TaxRateDto"];
export type TaxRatePage = components["schemas"]["TaxRateDtoPagedResult"];
export type CreateTaxRateRequest = components["schemas"]["CreateTaxRateRequest"];
export type UpdateTaxRateRequest = components["schemas"]["UpdateTaxRateRequest"];
export type AccountUser = components["schemas"]["UserDto"];
export type UpdateProfileRequest = components["schemas"]["UpdateProfileCommand"];
export type ChangeEmailRequest = components["schemas"]["ChangeEmailCommand"];
export type ChangePasswordRequest = components["schemas"]["ChangePasswordCommand"];
export type UserSession = components["schemas"]["UserSessionDto"];

// Burada sayfalı ayar listelerinin yalnızca belgeli URL durumunu taşıyorum.
export type SettingsListQuery = {
  pageNumber: number;
  pageSize: number;
};

// Burada Server Action sonuçlarını güvenli ve formların yeniden kullanabileceği biçimde tutuyorum.
export type SettingsActionState = {
  status: "idle" | "success" | "error";
  message?: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
};

export const initialSettingsActionState: SettingsActionState = { status: "idle" };
