export const ADMIN_ROLE = 2;
export const ACTIVE_USER_STATUS = 1;

// Burada Proxy'nin doğruladığı dönüş adresini yalnız sunucu render sınırına taşıyan iç header adını tanımlıyorum.
export const ADMIN_RETURN_TO_HEADER = "x-eleven-admin-return-to";

// Burada production cookie adlarını Secure, Path=/ ve domainsiz kullanım zorlayan __Host- önekiyle üretiyorum.
export function adminCookieNames(production: boolean) {
  const prefix = production ? "__Host-" : "";
  return {
    access: `${prefix}ecommerce_admin_access`,
    refresh: `${prefix}ecommerce_admin_refresh`,
  } as const;
}

const cookieNames = adminCookieNames(process.env.NODE_ENV === "production");
export const ADMIN_ACCESS_COOKIE = cookieNames.access;
export const ADMIN_REFRESH_COOKIE = cookieNames.refresh;

// Burada uygulanmış tüm Admin route köklerini güvenli dönüş hedefi ve iyimser Proxy kontrolü için tek listede tutuyorum.
export const PROTECTED_ADMIN_PREFIXES = [
  "/accounting",
  "/banners",
  "/brands",
  "/collections",
  "/contact-messages",
  "/coupons",
  "/customers",
  "/dashboard",
  "/inventory",
  "/managers",
  "/marketing",
  "/orders",
  "/products",
  "/settings",
] as const;
