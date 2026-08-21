import type { components, paths } from "@/generated/api";

export type ContactSubmissionOperation = paths["/api/contact-messages"]["post"];
export type ContactSubmissionRequest = components["schemas"]["SubmitContactMessageRequest"];
export type ContactSubmissionReceipt = components["schemas"]["ContactSubmissionReceiptDto"];
export type ContactMessageSubject = components["schemas"]["ContactMessageSubject"];

export type ContactFieldName = "name" | "email" | "phone" | "subject" | "orderNumber" | "message";

export type ContactDraft = {
  name: string;
  email: string;
  phone: string;
  subject: ContactMessageSubject;
  orderNumber: string;
  message: string;
};

export type ContactFieldErrors = Partial<Record<ContactFieldName, string[]>>;

export const CONTACT_SUBJECT_OPTIONS = [
  { value: 0, label: "Sipariş Takibi ve Durumu" },
  { value: 1, label: "Ürün Bilgisi ve Stok Danışmanlığı" },
  { value: 2, label: "İade, Değişim ve İptal Talebi" },
  { value: 3, label: "Kurumsal İş Birliği ve Toptan Satış" },
  { value: 4, label: "Öneri, Görüş veya Şikayet" },
  { value: 5, label: "Diğer Konular" },
] as const satisfies ReadonlyArray<{ value: ContactMessageSubject; label: string }>;

export const EMPTY_CONTACT_DRAFT: ContactDraft = {
  name: "",
  email: "",
  phone: "",
  subject: 0,
  orderNumber: "",
  message: "",
};
