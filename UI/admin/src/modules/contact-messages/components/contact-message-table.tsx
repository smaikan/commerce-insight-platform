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

// Burada gelen kutusunu tek semantik tablo yüzeyinde, PII kapsamını ad/e-postayla sınırlayarak sunuyorum.
export function ContactMessageTable({ page, query, admins }: { page: ContactMessagePage; query: ContactMessageListQuery; admins: readonly AssignableAdmin[] }) {
  if (page.items.length === 0) {
    const empty = contactMessageEmptyState(hasContactMessageFilters(query));
    return <div className="px-5 py-14 text-center"><h2 className="text-base font-semibold text-foreground">{empty.title}</h2><p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-muted">{empty.description}</p></div>;
  }
  return (
    <div className="overflow-x-auto bg-surface-strong">
      <table className="w-full min-w-[1180px] border-collapse text-left text-sm">
        <thead className="border-b border-border bg-surface-subtle/80 text-[11px] font-bold uppercase tracking-[0.08em] text-muted">
          <tr>
            <th scope="col" className="sticky left-0 z-10 w-[25%] bg-surface-subtle px-4 py-2.5">Referans ve gönderen</th>
            <th scope="col" className="px-3 py-2.5">Konu</th>
            <th scope="col" className="px-3 py-2.5">Durum</th>
            <th scope="col" className="px-3 py-2.5">Atanan yönetici</th>
            <th scope="col" className="px-3 py-2.5">Girilen sipariş no</th>
            <th scope="col" className="px-3 py-2.5">Son hareket</th>
            <th scope="col" className="px-3 py-2.5">Oluşturulma</th>
            <th scope="col" className="sticky right-0 z-10 bg-surface-subtle px-4 py-2.5 text-right">İşlem</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border/80">
          {page.items.map((message) => {
            const href = buildContactMessageDetailHref(message.id, query);
            return (
              <tr key={message.id} className="group bg-surface-strong align-middle hover:bg-primary-soft/20">
                <td className="sticky left-0 z-[1] max-w-72 bg-surface-strong px-4 py-3 group-hover:bg-primary-soft/20">
                  <Link href={href} className="block truncate font-mono text-sm font-bold text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">{message.referenceNumber}</Link>
                  <span className="mt-1 block truncate font-semibold text-foreground">{message.name}</span>
                  <span className="block truncate text-xs text-muted">{message.email}</span>
                  <span className={`mt-2 inline-flex rounded-md border px-2 py-0.5 text-xs font-bold lg:hidden ${contactMessageStatusClass(message.status)}`}>{contactMessageStatusLabel(message.status)}</span>
                </td>
                <td className="max-w-56 px-3 py-3 text-foreground"><span className="line-clamp-2">{contactMessageSubjectLabel(message.subject)}</span></td>
                <td className="px-3 py-3"><span className={`inline-flex whitespace-nowrap rounded-md border px-2 py-0.5 text-xs font-bold ${contactMessageStatusClass(message.status)}`}>{contactMessageStatusLabel(message.status)}</span></td>
                <td className="max-w-52 px-3 py-3"><span className="block truncate text-foreground">{adminDisplayName(message.assignedAdminUserId, admins)}</span></td>
                <td className="max-w-44 px-3 py-3"><span className="block truncate font-mono text-xs text-foreground">{message.providedOrderNumber || "—"}</span>{message.hasVerifiedOrder ? <span className="mt-1 block text-xs font-semibold text-success">API doğruladı</span> : null}</td>
                <td className="whitespace-nowrap px-3 py-3 font-mono text-xs tabular-nums text-muted">{formatContactMessageCompactDate(message.updatedAt ?? message.createdAt)}</td>
                <td className="whitespace-nowrap px-3 py-3 font-mono text-xs tabular-nums text-muted">{formatContactMessageCompactDate(message.createdAt)}</td>
                <td className="sticky right-0 z-[1] bg-surface-strong px-4 py-3 text-right group-hover:bg-primary-soft/20"><Link href={href} aria-label={`${message.referenceNumber} mesajını aç`} className="inline-flex min-h-10 items-center rounded-lg border border-border-strong px-3 font-semibold text-foreground hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Detay</Link></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
