import Link from "next/link";
import {
  adminDisplayName,
  contactMessageEmptyState,
  contactMessageStatusClass,
  contactMessageStatusLabel,
  contactMessageSubjectLabel,
  formatContactMessageCompactDate,
} from "@/modules/contact-messages/presentation";
import { buildContactMessageDetailHref, hasContactMessageFilters } from "@/modules/contact-messages/query";
import type { AssignableAdmin, ContactMessageListQuery, ContactMessagePage } from "@/modules/contact-messages/types";

// Burada gelen kutusu mesajlarını modern, okunabilir ve hiyerarşik bir tablo tasarımıyla sunuyorum.
export function ContactMessageTable({
  page,
  query,
  admins,
}: {
  page: ContactMessagePage;
  query: ContactMessageListQuery;
  admins: readonly AssignableAdmin[];
}) {
  if (page.items.length === 0) {
    const empty = contactMessageEmptyState(hasContactMessageFilters(query));
    return (
      <div className="px-5 py-16 text-center">
        <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-surface-subtle text-muted">
          <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="size-6">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75"
            />
          </svg>
        </div>
        <h2 className="mt-3 text-base font-semibold text-foreground">{empty.title}</h2>
        <p className="mx-auto mt-1 max-w-sm text-sm text-muted">{empty.description}</p>
        {hasContactMessageFilters(query) ? (
          <Link
            href="/contact-messages"
            className="mt-4 inline-flex min-h-9 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle"
          >
            Filtreleri Sıfırla
          </Link>
        ) : null}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto bg-surface">
      <table className="w-full min-w-[960px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/70 text-[11px] font-bold uppercase tracking-wider text-muted">
          <tr>
            <th scope="col" className="px-4 py-3">Referans & Gönderen</th>
            <th scope="col" className="px-3.5 py-3">Konu</th>
            <th scope="col" className="px-3.5 py-3">Durum</th>
            <th scope="col" className="px-3.5 py-3">Atanan Yönetici</th>
            <th scope="col" className="px-3.5 py-3">Sipariş No</th>
            <th scope="col" className="px-3.5 py-3">Son Hareket</th>
            <th scope="col" className="px-3.5 py-3">Oluşturulma</th>
            <th scope="col" className="px-4 py-3 text-right">İşlem</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/60">
          {page.items.map((message) => {
            const href = buildContactMessageDetailHref(message.id, query);
            const assignedAdminText = adminDisplayName(message.assignedAdminUserId, admins);
            const isUnassigned = !message.assignedAdminUserId || assignedAdminText === "Atanmamış";

            return (
              <tr key={message.id} className="group transition-colors hover:bg-surface-subtle/50">
                {/* Referans & Gönderen Bilgisi */}
                <td className="max-w-xs px-4 py-3.5 align-top">
                  <div className="flex items-center gap-2">
                    <Link
                      href={href}
                      className="inline-block font-mono text-xs font-bold text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary whitespace-nowrap"
                    >
                      {message.referenceNumber}
                    </Link>
                  </div>
                  <div className="mt-1 font-semibold text-sm text-foreground truncate">{message.name}</div>
                  <div className="text-xs text-muted truncate">{message.email}</div>
                </td>

                {/* Konu */}
                <td className="max-w-[200px] px-3.5 py-3.5 align-top">
                  <span className="inline-block font-medium text-xs text-foreground line-clamp-2">
                    {contactMessageSubjectLabel(message.subject)}
                  </span>
                </td>

                {/* Durum Rozeti */}
                <td className="px-3.5 py-3.5 align-top">
                  <span
                    className={`inline-flex items-center rounded-md border px-2 py-0.5 text-xs font-semibold whitespace-nowrap ${contactMessageStatusClass(
                      message.status,
                    )}`}
                  >
                    {contactMessageStatusLabel(message.status)}
                  </span>
                </td>

                {/* Atanan Yönetici */}
                <td className="max-w-[180px] px-3.5 py-3.5 align-top">
                  {isUnassigned ? (
                    <span className="text-xs italic text-muted whitespace-nowrap">Atanmamış</span>
                  ) : (
                    <span className="block truncate text-xs font-medium text-foreground whitespace-nowrap">
                      {assignedAdminText}
                    </span>
                  )}
                </td>

                {/* Girilen Sipariş Numarası ve Doğrulama Durumu */}
                <td className="max-w-[150px] px-3.5 py-3.5 align-top">
                  {message.providedOrderNumber ? (
                    <div>
                      <span className="block font-mono text-xs font-semibold text-foreground whitespace-nowrap">
                        {message.providedOrderNumber}
                      </span>
                      {message.hasVerifiedOrder ? (
                        <span className="mt-0.5 flex items-center gap-1 text-[11px] font-semibold text-success whitespace-nowrap">
                          <svg aria-hidden="true" viewBox="0 0 20 20" fill="currentColor" className="size-3 shrink-0">
                            <path
                              fillRule="evenodd"
                              d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z"
                              clipRule="evenodd"
                            />
                          </svg>
                          <span>Doğrulandı</span>
                        </span>
                      ) : null}
                    </div>
                  ) : (
                    <span className="text-xs text-muted whitespace-nowrap">—</span>
                  )}
                </td>

                {/* Son Hareket */}
                <td className="whitespace-nowrap px-3.5 py-3.5 align-top font-mono text-xs tabular-nums text-muted">
                  {formatContactMessageCompactDate(message.updatedAt ?? message.createdAt)}
                </td>

                {/* Oluşturulma Tarihi */}
                <td className="whitespace-nowrap px-3.5 py-3.5 align-top font-mono text-xs tabular-nums text-muted">
                  {formatContactMessageCompactDate(message.createdAt)}
                </td>

                {/* Detay Eylemi */}
                <td className="whitespace-nowrap px-4 py-3.5 align-top text-right">
                  <Link
                    href={href}
                    aria-label={`${message.referenceNumber} mesajını aç`}
                    className="inline-flex min-h-8 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground shadow-2xs transition-colors hover:border-primary hover:text-primary hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary"
                  >
                    Detay
                  </Link>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
