export const ADMIN_ROLE = 2;
export const ACTIVE_USER_STATUS = 1;
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

export const PROTECTED_ADMIN_PREFIXES = ["/dashboard", "/products", "/orders"] as const;
