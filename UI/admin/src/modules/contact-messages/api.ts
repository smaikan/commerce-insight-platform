import "server-only";

import { apiRequest } from "@/lib/api/client";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  AddContactMessageNoteRequest,
  AssignableAdmin,
  AssignableAdminPage,
  AssignContactMessageRequest,
  ChangeContactMessageStatusRequest,
  ContactMessageDetail,
  ContactMessageListQuery,
  ContactMessagePage,
  ReplyContactMessageRequest,
} from "@/modules/contact-messages/types";

// Burada iletişim mesajı listesini yalnız belgelenmiş filtre ve sayfalama parametreleriyle getiriyorum.
export function getContactMessages(query: ContactMessageListQuery, session: AdminSession): Promise<ContactMessagePage> {
  const params = new URLSearchParams({
    PageNumber: String(query.pageNumber),
    PageSize: String(query.pageSize),
  });
  if (query.search) params.set("Search", query.search);
  if (query.status !== undefined) params.set("Status", String(query.status));
  if (query.subject !== undefined) params.set("Subject", String(query.subject));
  if (query.assignedAdminUserId) params.set("AssignedAdminUserId", query.assignedAdminUserId);
  if (query.createdFromApiUtc) params.set("CreatedFromUtc", query.createdFromApiUtc);
  if (query.createdToApiUtc) params.set("CreatedToUtc", query.createdToApiUtc);
  return apiRequest(`/api/contact-messages?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada tam iletişim mesajı detayını AdminOnly endpointinden no-store server sınırında okuyorum.
export function getContactMessage(messageId: string, session: AdminSession): Promise<ContactMessageDetail> {
  return apiRequest(`/api/contact-messages/${encodeURIComponent(messageId)}`, { accessToken: session.accessToken });
}

// Burada atanabilir aktif yöneticilerin bütün belgelenmiş sayfalarını assignment seçicisi için topluyorum.
export async function getAssignableAdmins(session: AdminSession): Promise<AssignableAdmin[]> {
  const firstPage = await getAssignableAdminPage(1, session);
  const remaining = firstPage.totalPages > 1
    ? await Promise.all(
        Array.from({ length: firstPage.totalPages - 1 }, (_, index) => getAssignableAdminPage(index + 2, session)),
      )
    : [];
  return [firstPage, ...remaining]
    .flatMap((page) => page.items)
    .sort((left, right) => `${left.firstName} ${left.lastName}`.localeCompare(`${right.firstName} ${right.lastName}`, "tr"));
}

// Burada yalnız aktif Admin rolündeki kullanıcıların bir sayfasını yayımlanmış Users filtresiyle getiriyorum.
function getAssignableAdminPage(pageNumber: number, session: AdminSession): Promise<AssignableAdminPage> {
  const params = new URLSearchParams({
    PageNumber: String(pageNumber),
    PageSize: "100",
    Role: "2",
    Status: "1",
  });
  return apiRequest(`/api/users?${params.toString()}`, { accessToken: session.accessToken });
}

// Burada status mutasyonunu concurrency tokenla belgelenmiş PATCH endpointine iletiyorum.
export function changeContactMessageStatus(
  messageId: string,
  request: ChangeContactMessageStatusRequest,
  session: AdminSession,
): Promise<ContactMessageDetail> {
  return apiRequest(`/api/contact-messages/${encodeURIComponent(messageId)}/status`, {
    method: "PATCH",
    body: request,
    accessToken: session.accessToken,
  });
}

// Burada atama veya atama kaldırma intentini concurrency tokenla ayrı endpointine gönderiyorum.
export function assignContactMessage(
  messageId: string,
  request: AssignContactMessageRequest,
  session: AdminSession,
): Promise<ContactMessageDetail> {
  return apiRequest(`/api/contact-messages/${encodeURIComponent(messageId)}/assignment`, {
    method: "PATCH",
    body: request,
    accessToken: session.accessToken,
  });
}

// Burada append-only dahili notu concurrency tokenla mesaj audit akışına ekliyorum.
export function addContactMessageNote(
  messageId: string,
  request: AddContactMessageNoteRequest,
  session: AdminSession,
): Promise<ContactMessageDetail> {
  return apiRequest(`/api/contact-messages/${encodeURIComponent(messageId)}/notes`, {
    method: "POST",
    body: request,
    accessToken: session.accessToken,
  });
}

// Burada müşteri yanıtını aynı intentte korunan Idempotency-Key ile outbox kuyruğuna gönderiyorum.
export function replyToContactMessage(
  messageId: string,
  request: ReplyContactMessageRequest,
  idempotencyKey: string,
  session: AdminSession,
): Promise<ContactMessageDetail> {
  return apiRequest(`/api/contact-messages/${encodeURIComponent(messageId)}/replies`, {
    method: "POST",
    body: request,
    headers: { "Idempotency-Key": idempotencyKey },
    accessToken: session.accessToken,
  });
}
