import type { LoginPayload, RegisterPayload } from "@/modules/auth/contracts";

export type AuthFieldErrors = Partial<Record<"email" | "password" | "confirmPassword" | "firstName" | "lastName" | "phoneNumber" | "legalConsent", string>>;
export type AuthFormValues = Partial<Record<"email" | "firstName" | "lastName" | "phoneNumber", string>> & { legalConsent?: boolean };

type ValidationResult<T> = { success: true; data: T } | { success: false; errors: AuthFieldErrors; values: AuthFormValues };

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// Burada login girdisini API sınırlarıyla aynı uzunluk ve biçim kurallarına göre sunucuda doğruluyorum.
export function validateLogin(formData: FormData): ValidationResult<Omit<LoginPayload, "deviceName">> {
  const email = readText(formData, "email").trim().toLowerCase();
  const password = readText(formData, "password");
  const errors: AuthFieldErrors = {};

  if (!email) errors.email = "E-posta adresinizi girin.";
  else if (email.length > 320 || !EMAIL_PATTERN.test(email)) errors.email = "Geçerli bir e-posta adresi girin.";
  if (!password) errors.password = "Şifrenizi girin.";
  else if (password.length > 128) errors.password = "Şifre en fazla 128 karakter olabilir.";

  return Object.keys(errors).length
    ? { success: false, errors, values: { email } }
    : { success: true, data: { email, password } };
}

// Burada kayıt alanlarını backend validator sınırlarıyla eşleyip doğrulama şifresini yalnızca Storefront'ta kontrol ediyorum.
export function validateRegister(formData: FormData): ValidationResult<RegisterPayload> {
  const firstName = readText(formData, "firstName").trim();
  const lastName = readText(formData, "lastName").trim();
  const email = readText(formData, "email").trim().toLowerCase();
  const phoneNumber = readText(formData, "phoneNumber").trim();
  const password = readText(formData, "password");
  const confirmPassword = readText(formData, "confirmPassword");
  // Burada yalnızca kutucuğun beklenen değerini yasal onay sayarak istemci doğrulamasının atlanmasını engelliyorum.
  const legalConsent = formData.get("legalConsent") === "accepted";
  const errors: AuthFieldErrors = {};

  if (!firstName) errors.firstName = "Adınızı girin.";
  else if (firstName.length > 100) errors.firstName = "Ad en fazla 100 karakter olabilir.";
  if (!lastName) errors.lastName = "Soyadınızı girin.";
  else if (lastName.length > 100) errors.lastName = "Soyad en fazla 100 karakter olabilir.";
  if (!email) errors.email = "E-posta adresinizi girin.";
  else if (email.length > 320 || !EMAIL_PATTERN.test(email)) errors.email = "Geçerli bir e-posta adresi girin.";
  if (phoneNumber.length > 30) errors.phoneNumber = "Telefon numarası en fazla 30 karakter olabilir.";
  if (!password) errors.password = "Şifrenizi oluşturun.";
  else if (password.length < 6) errors.password = "Şifre en az 6 karakter olmalı.";
  else if (password.length > 128) errors.password = "Şifre en fazla 128 karakter olabilir.";
  if (!confirmPassword) errors.confirmPassword = "Şifrenizi tekrar girin.";
  else if (password !== confirmPassword) errors.confirmPassword = "Şifreler birbiriyle eşleşmiyor.";
  if (!legalConsent) errors.legalConsent = "Devam etmek için üyelik koşullarını onaylayın.";

  const values = { firstName, lastName, email, phoneNumber, legalConsent };
  return Object.keys(errors).length
    ? { success: false, errors, values }
    : { success: true, data: { firstName, lastName, email, password, phoneNumber: phoneNumber || null } };
}

function readText(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value : "";
}
