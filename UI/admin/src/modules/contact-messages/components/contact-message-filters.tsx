import Link from "next/link";
import {
  adminDisplayName,
  contactMessageStatusOptions,
  contactMessageSubjectLabel,
  contactMessageSubjectOptions,
} from "@/modules/contact-messages/presentation";
import {
  buildContactStatusTabHref,
  buildRemoveContactMessageFilterHref,
  hasContactMessageFilters,
} from "@/modules/contact-messages/query";
import type { AssignableAdmin, ContactMessageListQuery } from "@/modules/contact-messages/types";

const selectWrapperClass = "relative block min-w-0";
const selectControlClass =
  "min-h-9 w-full appearance-none rounded-lg border border-border-strong bg-surface-strong py-1.5 pl-3 pr-8 text-sm text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20";
const dateControlClass =
  "min-h-9 w-full rounded-lg border border-border-strong bg-surface-strong px-2.5 py-1 text-xs text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20";

// Burada iletişim gelen kutusu filtrelerini durum sekmeleri, entegre arama ve kompakt filtre çubuğuyla sunuyorum.
export function ContactMessageFilters({
  query,
  admins,
}: {
  query: ContactMessageListQuery;
  admins: readonly AssignableAdmin[];
}) {
  const isAnyFilterActive = hasContactMessageFilters(query);
  const activeSubjectName =
    query.subject !== undefined ? contactMessageSubjectLabel(query.subject) : undefined;
  const activeAdminName =
    query.assignedAdminUserId !== undefined
      ? adminDisplayName(query.assignedAdminUserId, admins)
      : undefined;

  const currentTabStatus = query.status;

  const statusTabs = [
    { id: "all", label: "Tümü", value: undefined, href: buildContactStatusTabHref(query, undefined) },
    ...contactMessageStatusOptions.map((opt) => ({
      id: String(opt.value),
      label: opt.label,
      value: opt.value,
      href: buildContactStatusTabHref(query, opt.value),
    })),
  ];

  return (
    <div className="border-b border-border bg-surface">
      {/* Durum / Görünüm Hızlı Filtre Sekmeleri */}
      <nav
        aria-label="İletişim mesajı durum sekmeleri"
        className="flex items-center gap-1 overflow-x-auto border-b border-border px-4 pt-2 sm:px-5"
      >
        {statusTabs.map((tab) => {
          const isActive = currentTabStatus === tab.value;
          return (
            <Link
              key={tab.id}
              href={tab.href}
              className={`inline-flex shrink-0 items-center border-b-2 px-3 py-2 text-xs font-semibold transition-colors ${
                isActive
                  ? "border-primary text-primary"
                  : "border-transparent text-muted hover:border-border-strong hover:text-foreground"
              }`}
            >
              {tab.label}
            </Link>
          );
        })}
      </nav>

      {/* Ana Filtreleme Formu */}
      <form action="/contact-messages" method="get" className="p-3.5 sm:p-4">
        {/* Seçili durum sekmesini form submit'inde koru */}
        {query.status !== undefined ? <input type="hidden" name="status" value={query.status} /> : null}

        <div className="grid gap-2.5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {/* Arama Alanı */}
          <div className="sm:col-span-2 md:col-span-3 lg:col-span-2 xl:col-span-2">
            <label htmlFor="contact-search" className="sr-only">Mesaj Ara</label>
            <div className="relative">
              <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 fill-none stroke-muted stroke-2"
              >
                <circle cx="11" cy="11" r="7" />
                <path d="m16 16 4 4" strokeLinecap="round" />
              </svg>
              <input
                id="contact-search"
                name="search"
                type="search"
                defaultValue={query.search ?? ""}
                placeholder="Referans, ad, e-posta veya sipariş no..."
                className="min-h-9 w-full rounded-lg border border-border-strong bg-surface-strong pl-9 pr-3 text-sm text-foreground outline-none transition-colors placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-primary/20"
                autoComplete="off"
              />
            </div>
          </div>

          {/* Konu */}
          <div className={selectWrapperClass}>
            <label htmlFor="contact-subject" className="sr-only">Konu</label>
            <select
              id="contact-subject"
              name="subject"
              defaultValue={query.subject ?? ""}
              className={selectControlClass}
            >
              <option value="">Tüm Konular</option>
              {contactMessageSubjectOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Atanan Yönetici */}
          <div className={selectWrapperClass}>
            <label htmlFor="contact-admin" className="sr-only">Atanan Yönetici</label>
            <select
              id="contact-admin"
              name="assignedAdminUserId"
              defaultValue={query.assignedAdminUserId ?? ""}
              className={selectControlClass}
            >
              <option value="">Tüm Yöneticiler</option>
              {admins.map((admin) => (
                <option key={admin.id} value={admin.id}>
                  {admin.firstName} {admin.lastName} ({admin.id})
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>

          {/* Başlangıç Tarihi */}
          <div className="relative block min-w-0">
            <label htmlFor="contact-date-from" className="sr-only">Başlangıç Tarihi</label>
            <div className="relative">
              <input
                id="contact-date-from"
                name="createdFromUtc"
                type="date"
                defaultValue={query.createdFromUtc ?? ""}
                className={dateControlClass}
                aria-invalid={Boolean(query.dateError)}
                title="Başlangıç Tarihi (UTC)"
              />
            </div>
          </div>

          {/* Bitiş Tarihi */}
          <div className="relative block min-w-0">
            <label htmlFor="contact-date-to" className="sr-only">Bitiş Tarihi</label>
            <div className="relative">
              <input
                id="contact-date-to"
                name="createdToUtc"
                type="date"
                defaultValue={query.createdToUtc ?? ""}
                className={dateControlClass}
                aria-invalid={Boolean(query.dateError)}
                title="Bitiş Tarihi (UTC)"
              />
            </div>
          </div>

          {/* Sayfa Boyutu & Aksiyon Butonları */}
          <div className="flex items-center gap-2 sm:col-span-2 md:col-span-3 lg:col-span-4 xl:col-span-6 justify-end">
            <div className="w-36">
              <label htmlFor="contact-page-size" className="sr-only">Sayfa Boyutu</label>
              <div className={selectWrapperClass}>
                <select
                  id="contact-page-size"
                  name="pageSize"
                  defaultValue={query.pageSize}
                  className={selectControlClass}
                >
                  {[10, 20, 50, 100].map((size) => (
                    <option key={size} value={size}>
                      {size} mesaj / sayfa
                    </option>
                  ))}
                </select>
                <SelectChevron />
              </div>
            </div>

            <button
              type="submit"
              className="inline-flex min-h-9 cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-primary px-4 text-xs font-semibold text-white shadow-xs transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
            >
              <svg aria-hidden="true" viewBox="0 0 20 20" fill="currentColor" className="size-3.5">
                <path
                  fillRule="evenodd"
                  d="M2.628 1.601C5.028 1.206 7.49 1 10 1s4.973.206 7.372.601a.75.75 0 0 1 .628.74v2.288a2.25 2.25 0 0 1-.659 1.59l-4.682 4.683a2.25 2.25 0 0 0-.659 1.59v3.037c0 .684-.31 1.33-.844 1.757l-1.937 1.55A.75.75 0 0 1 8 18.25v-5.757a2.25 2.25 0 0 0-.659-1.591L2.659 6.22A2.25 2.25 0 0 1 2 4.629V2.34a.75.75 0 0 1 .628-.74Z"
                  clipRule="evenodd"
                />
              </svg>
              <span>Filtrele</span>
            </button>

            {isAnyFilterActive ? (
              <Link
                href="/contact-messages"
                className="inline-flex min-h-9 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-medium text-foreground transition-colors hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
              >
                Temizle
              </Link>
            ) : null}
          </div>
        </div>

        {/* Tarih Hatası Bildirimi */}
        {query.dateError ? (
          <p id="contact-date-error" role="alert" className="mt-2.5 text-xs font-semibold text-danger">
            {query.dateError}
          </p>
        ) : null}

        {/* Aktif Filtre Çipleri */}
        {isAnyFilterActive ? (
          <div className="mt-3 flex flex-wrap items-center gap-1.5 border-t border-border/60 pt-2.5">
            <span className="text-[11px] font-semibold uppercase tracking-wider text-muted">Aktif:</span>

            {query.search ? (
              <FilterChip
                label={`Arama: "${query.search}"`}
                removeHref={buildRemoveContactMessageFilterHref(query, "search")}
                ariaLabel="Arama filtresini kaldır"
              />
            ) : null}

            {query.subject !== undefined && activeSubjectName ? (
              <FilterChip
                label={`Konu: ${activeSubjectName}`}
                removeHref={buildRemoveContactMessageFilterHref(query, "subject")}
                ariaLabel="Konu filtresini kaldır"
              />
            ) : null}

            {query.assignedAdminUserId && activeAdminName ? (
              <FilterChip
                label={`Yönetici: ${activeAdminName}`}
                removeHref={buildRemoveContactMessageFilterHref(query, "assignedAdminUserId")}
                ariaLabel="Yönetici filtresini kaldır"
              />
            ) : null}

            {query.createdFromUtc || query.createdToUtc ? (
              <FilterChip
                label={`Tarih: ${query.createdFromUtc || "Başlangıç"} → ${query.createdToUtc || "Bitiş"}`}
                removeHref={buildRemoveContactMessageFilterHref(query, "dateRange")}
                ariaLabel="Tarih aralığı filtresini kaldır"
              />
            ) : null}

            <Link
              href="/contact-messages"
              className="ml-1 text-xs font-semibold text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary"
            >
              Tümünü Temizle
            </Link>
          </div>
        ) : null}
      </form>
    </div>
  );
}

// Burada select elemanları için aşağı ok ikonu sunuyorum.
function SelectChevron() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 20 20"
      fill="currentColor"
      className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted"
    >
      <path
        fillRule="evenodd"
        d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
        clipRule="evenodd"
      />
    </svg>
  );
}

// Burada aktif filtrenin tek tıkla kaldırılabilmesini sağlayan kompakt çip bileşenini sunuyorum.
function FilterChip({
  label,
  removeHref,
  ariaLabel,
}: {
  label: string;
  removeHref: string;
  ariaLabel: string;
}) {
  return (
    <span className="inline-flex items-center gap-1 rounded-md border border-border-strong/70 bg-surface-subtle py-0.5 pl-2 pr-1 text-xs font-medium text-foreground">
      <span>{label}</span>
      <Link
        href={removeHref}
        aria-label={ariaLabel}
        className="inline-flex size-3.5 items-center justify-center rounded text-muted transition-colors hover:bg-surface-strong hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary"
      >
        <svg aria-hidden="true" viewBox="0 0 14 14" className="size-2.5 stroke-current stroke-2 fill-none">
          <path d="m3 3 8 8M11 3 3 11" strokeLinecap="round" />
        </svg>
      </Link>
    </span>
  );
}
