import type { components, paths } from "@/generated/api";

// Burada iletişim mesajı wire modellerini generated OpenAPI şemalarına bağlıyorum.
export type ContactMessageSummary = components["schemas"]["ContactMessageSummaryDto"];
export type ContactMessagePage = components["schemas"]["ContactMessageSummaryDtoPagedResult"];
export type ContactMessageDetail = components["schemas"]["ContactMessageDetailDto"];
export type ContactMessageActivity = components["schemas"]["ContactMessageActivityDto"];
export type ContactMessageReply = components["schemas"]["ContactMessageReplyDto"];
export type ContactMessageStatus = components["schemas"]["ContactMessageStatus"];
export type ContactMessageSubject = components["schemas"]["ContactMessageSubject"];
export type ContactMessageActivityType = components["schemas"]["ContactMessageActivityType"];
export type ContactReplyDeliveryStatus = components["schemas"]["ContactReplyDeliveryStatus"];
export type AssignableAdmin = components["schemas"]["AdminUserDto"];
export type AssignableAdminPage = components["schemas"]["AdminUserDtoPagedResult"];

// Burada mutation request tiplerini elle çoğaltmadan generated component sözleşmelerinden alıyorum.
export type ChangeContactMessageStatusRequest = components["schemas"]["ChangeContactMessageStatusRequest"];
export type AssignContactMessageRequest = components["schemas"]["AssignContactMessageRequest"];
export type AddContactMessageNoteRequest = components["schemas"]["AddContactMessageNoteRequest"];
export type ReplyContactMessageRequest = components["schemas"]["ReplyContactMessageRequest"];

// Burada liste endpointinin generated query tipini API katmanında doğrudan kullanıma açıyorum.
export type ContactMessageApiQuery = NonNullable<paths["/api/contact-messages"]["get"]["parameters"]["query"]>;

// Burada URL girdisi, API UTC sınırı ve alan hatasını aynı liste sorgusu bağlamında tutuyorum.
export type ContactMessageListQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  status?: ContactMessageStatus;
  subject?: ContactMessageSubject;
  assignedAdminUserId?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
  createdFromApiUtc?: string;
  createdToApiUtc?: string;
  dateError?: string;
};

// Burada Client Component sınırına yalnız concurrency için gereken dar kayıt görünümünü taşıyorum.
export type ContactMessageMutationSnapshot = {
  concurrencyToken: string;
  status: ContactMessageStatus;
  assignedAdminUserId?: string | null;
  updatedAt?: string | null;
};

// Burada bütün iletişim mutasyonlarının güvenli ve seri hale getirilebilir sonuç birliğini tanımlıyorum.
export type ContactMessageActionResult =
  | { status: "idle" }
  | { status: "success"; message: string; snapshot: ContactMessageMutationSnapshot }
  | {
      status: "conflict";
      message: string;
      code: "concurrency_conflict";
      snapshot?: ContactMessageMutationSnapshot;
      traceId?: string;
    }
  | {
      status: "error";
      message: string;
      code?: string;
      traceId?: string;
      retryAfter?: string;
      fieldErrors?: Record<string, string[]>;
    };

export const initialContactMessageActionResult: ContactMessageActionResult = { status: "idle" };
