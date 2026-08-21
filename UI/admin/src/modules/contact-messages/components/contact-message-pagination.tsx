import { AdminPagination } from "@/modules/admin-shell/components/admin-pagination";
import { buildContactMessageListHref } from "@/modules/contact-messages/query";
import type { ContactMessageListQuery, ContactMessagePage } from "@/modules/contact-messages/types";

// Burada server-side iletişim sayfalamasında geçerli filtreleri doğrudan atlama formunda da koruyorum.
export function ContactMessagePagination({ page, query }: { page: ContactMessagePage; query: ContactMessageListQuery }) {
  const hiddenFields: Array<{ name: string; value: string | number }> = [];
  const candidates: Array<{ name: string; value: string | number } | null> = [
    query.pageSize !== 20 ? { name: "pageSize", value: query.pageSize } : null,
    query.search ? { name: "search", value: query.search } : null,
    query.status !== undefined ? { name: "status", value: query.status } : null,
    query.subject !== undefined ? { name: "subject", value: query.subject } : null,
    query.assignedAdminUserId ? { name: "assignedAdminUserId", value: query.assignedAdminUserId } : null,
    query.createdFromUtc ? { name: "createdFromUtc", value: query.createdFromUtc } : null,
    query.createdToUtc ? { name: "createdToUtc", value: query.createdToUtc } : null,
  ];
  for (const field of candidates) if (field) hiddenFields.push(field);
  return <AdminPagination action="/contact-messages" ariaLabel="İletişim mesajı sayfaları" buildHref={(number) => buildContactMessageListHref(query, number)} hiddenFields={hiddenFields} itemLabel="mesaj" pageNumber={page.pageNumber} pageSize={page.pageSize} totalCount={page.totalCount} totalPages={page.totalPages} />;
}
