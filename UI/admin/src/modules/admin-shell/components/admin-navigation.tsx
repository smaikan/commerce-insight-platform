"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  getCurrentNavigationHref,
  navigationSections,
  navigationStatusLabel,
  type NavigationSection,
} from "@/modules/admin-shell/navigation";
import {
  formatWorkQueueCount,
  getWorkQueueAccessibleLabel,
  getWorkQueueCount,
} from "@/modules/admin-shell/work-queue";
import {
  type AdminNavigationMode,
  useAdminWorkQueueSummary,
} from "@/modules/admin-shell/use-admin-work-queue-summary";
import type { AdminWorkQueueSummaryData } from "@/modules/dashboard/types";

// Burada detay rotalarında da ilgili ana menü öğesini seçili kabul ediyorum.
function isCurrentItem(pathname: string, href: string | undefined): boolean {
  return Boolean(href && (pathname === href || pathname.startsWith(`${href}/`)));
}

// Burada etkin ve geliştirme aşamasındaki öğeleri aynı yoğunlukta, gereksiz durum tekrarları olmadan çiziyorum.
function NavigationItems({
  section,
  activeHref,
  summary,
  onNavigate,
}: {
  section: NavigationSection;
  activeHref?: string;
  summary: AdminWorkQueueSummaryData | null;
  onNavigate?: () => void;
}) {
  return (
    <ul className="mt-1 space-y-0.5">
      {section.items.map((item) => {
        const isActive = item.href === activeHref;
        const notificationCount = getWorkQueueCount(summary, item.workQueueKey);
        const accessibleLabel = getWorkQueueAccessibleLabel(item.label, item.workQueueKey, notificationCount);

        return (
          <li key={item.label}>
            {item.href ? (
              <Link
                href={item.href}
                aria-current={isActive ? "page" : undefined}
                aria-label={accessibleLabel}
                onClick={onNavigate}
                className={`flex min-h-11 flex-col justify-center rounded-md border-l-2 py-2 pl-6 pr-3 text-sm transition-colors lg:min-h-9 ${
                  isActive
                    ? "border-blue-200 bg-sidebar-active/75 font-semibold text-sidebar-foreground"
                    : "border-transparent font-medium text-sidebar-foreground hover:bg-sidebar-hover"
                }`}
              >
                <span className="flex min-w-0 items-center gap-2">
                  <span className="min-w-0 flex-1 whitespace-normal break-words leading-5">{item.label}</span>
                  {notificationCount > 0 ? (
                    <span
                      aria-hidden="true"
                      title={`${notificationCount} kayıt`}
                      className="inline-flex min-w-6 shrink-0 items-center justify-center rounded-full bg-warning px-1.5 py-0.5 text-[11px] font-bold leading-4 text-white tabular-nums"
                    >
                      {formatWorkQueueCount(notificationCount)}
                    </span>
                  ) : null}
                </span>
                {item.status !== "available" && section.status !== item.status ? (
                  <span className="mt-0.5 block text-[11px] font-normal leading-4 text-sidebar-muted">
                    {navigationStatusLabel(item.status)}
                  </span>
                ) : null}
              </Link>
            ) : (
              <span
                aria-disabled="true"
                className="block min-h-10 rounded-md border-l-2 border-transparent py-2 pl-6 pr-3 text-sm leading-5 text-sidebar-muted"
              >
                <span className="block whitespace-normal break-words">{item.label}</span>
                {section.status !== item.status ? (
                  <span className="mt-0.5 block text-[11px] leading-4 text-sidebar-muted">
                    {navigationStatusLabel(item.status)}
                  </span>
                ) : null}
              </span>
            )}
          </li>
        );
      })}
    </ul>
  );
}

// Burada sık kullanılan modülleri doğrudan görünür, uzun gelecek bölümlerini ise kapalı birer alt ağaç olarak sunuyorum.
export function AdminNavigation({
  initialSummary,
  initialUnavailable,
  mode,
  onNavigate,
}: {
  initialSummary: AdminWorkQueueSummaryData | null;
  initialUnavailable: boolean;
  mode: AdminNavigationMode;
  onNavigate?: () => void;
}) {
  const pathname = usePathname();
  const activeHref = getCurrentNavigationHref(pathname);
  const { summary, unavailable } = useAdminWorkQueueSummary(initialSummary, initialUnavailable, mode);

  return (
    <nav aria-label="Ana navigasyon" className="space-y-3 px-3 pb-6">
      {navigationSections.map((section, sectionIndex) => {
        const sectionId = `navigation-section-${sectionIndex}`;
        const hasCurrentItem = section.items.some((item) => isCurrentItem(pathname, item.href));

        if (section.collapsible) {
          return (
            <details key={section.label} className="group/navigation border-t border-sidebar-border/70 pt-3" open={hasCurrentItem}>
              <summary className="flex min-h-11 cursor-pointer list-none items-start gap-2 rounded-md px-3 py-2 text-[10px] font-bold uppercase tracking-[0.12em] text-sidebar-muted hover:bg-sidebar-hover hover:text-sidebar-foreground [&::-webkit-details-marker]:hidden lg:min-h-10">
                <span className="min-w-0 flex-1">
                  <span className="block whitespace-normal break-words leading-4">{section.label}</span>
                  {section.status ? (
                    <span className="mt-1 block text-[11px] font-medium normal-case tracking-normal text-sidebar-muted">
                      {navigationStatusLabel(section.status)}
                    </span>
                  ) : null}
                </span>
                <svg aria-hidden="true" viewBox="0 0 20 20" className="mt-0.5 size-4 shrink-0 transition-transform group-open/navigation:rotate-180">
                  <path d="m6 8 4 4 4-4" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" />
                </svg>
              </summary>
              <NavigationItems section={section} activeHref={activeHref} summary={summary} onNavigate={onNavigate} />
            </details>
          );
        }

        return (
          <section key={section.label} aria-labelledby={sectionId} className={sectionIndex === 0 ? "" : "border-t border-sidebar-border/70 pt-3"}>
            <h2 id={sectionId} className="flex min-h-7 items-center px-3 text-[10px] font-bold uppercase tracking-[0.12em] text-sidebar-muted">
              <span>{section.label}</span>
            </h2>
            <NavigationItems section={section} activeHref={activeHref} summary={summary} onNavigate={onNavigate} />
          </section>
        );
      })}
      {unavailable ? (
        <p role="status" className="mx-3 rounded-md border border-warning/30 bg-warning/10 px-3 py-2 text-xs leading-5 text-sidebar-foreground">
          Bildirim sayaçları geçici olarak güncellenemiyor; son başarılı değerler korunuyor.
        </p>
      ) : null}
    </nav>
  );
}
