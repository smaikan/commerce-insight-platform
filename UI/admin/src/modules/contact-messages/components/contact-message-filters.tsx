import Link from "next/link";
import { contactMessageStatusOptions, contactMessageSubjectOptions, assignableAdminLabel } from "@/modules/contact-messages/presentation";
import { hasContactMessageFilters } from "@/modules/contact-messages/query";
import type { AssignableAdmin, ContactMessageListQuery } from "@/modules/contact-messages/types";

const controlClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30";

// Burada yalnız API'nin yayımladığı iletişim filtrelerini kalıcı etiketlerle sunuyorum.
export function ContactMessageFilters({ query, admins }: { query: ContactMessageListQuery; admins: readonly AssignableAdmin[] }) {
  return (
    <form action="/contact-messages" method="get" className="border-b border-border bg-surface-subtle/60 p-4 sm:p-5">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <label className="xl:col-span-2">
          <span className="mb-1.5 block text-xs font-semibold text-muted">Referans, gönderen veya sipariş ara</span>
          <input name="search" type="search" defaultValue={query.search ?? ""} placeholder="Referans, ad, e-posta veya sipariş no" autoComplete="off" className={controlClass} />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Durum</span>
          <select name="status" defaultValue={query.status ?? ""} className={controlClass}>
            <option value="">Tüm durumlar</option>
            {contactMessageStatusOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Konu</span>
          <select name="subject" defaultValue={query.subject ?? ""} className={controlClass}>
            <option value="">Tüm konular</option>
            {contactMessageSubjectOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Atanan yönetici</span>
          <select name="assignedAdminUserId" defaultValue={query.assignedAdminUserId ?? ""} className={controlClass}>
            <option value="">Tüm yöneticiler</option>
            {admins.map((admin) => <option key={admin.id} value={admin.id}>{assignableAdminLabel(admin)}</option>)}
          </select>
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Başlangıç tarihi (UTC)</span>
          <input name="createdFromUtc" type="date" defaultValue={query.createdFromUtc ?? ""} className={controlClass} aria-invalid={Boolean(query.dateError)} aria-describedby={query.dateError ? "contact-date-error" : undefined} />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Bitiş tarihi (UTC)</span>
          <input name="createdToUtc" type="date" defaultValue={query.createdToUtc ?? ""} className={controlClass} aria-invalid={Boolean(query.dateError)} aria-describedby={query.dateError ? "contact-date-error" : undefined} />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-semibold text-muted">Sayfa boyutu</span>
          <select name="pageSize" defaultValue={query.pageSize} className={controlClass}>
            {[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size} mesaj / sayfa</option>)}
          </select>
        </label>
      </div>
      {query.dateError ? <p id="contact-date-error" role="alert" className="mt-3 text-sm font-semibold text-danger">{query.dateError}</p> : null}
      <div className="mt-4 flex flex-wrap justify-end gap-2">
        {hasContactMessageFilters(query) ? <Link href="/contact-messages" className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle">Filtreleri temizle</Link> : null}
        <button type="submit" className="min-h-10 cursor-pointer rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Uygula</button>
      </div>
    </form>
  );
}
