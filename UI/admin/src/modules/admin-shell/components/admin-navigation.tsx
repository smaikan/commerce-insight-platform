"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  navigationSections,
  navigationStatusLabel,
} from "@/modules/admin-shell/navigation";

function NavigationGlyph({ sectionIndex }: { sectionIndex: number }) {
  const paths = [
    "M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z",
    "M6 7h12l1 13H5L6 7Zm3 0V5a3 3 0 0 1 6 0v2",
    "M4 6h16v12H4zM8 10h8M8 14h5",
    "M5 5h14v14H5zM9 9h6v6H9z",
    "M4 8h16M7 4h10l3 4v12H4V8l3-4Z",
    "M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M18.4 5.6l-2.1 2.1M7.7 16.3l-2.1 2.1M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z",
  ];

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="size-4 shrink-0 fill-none stroke-current stroke-[1.7]"
    >
      <path d={paths[sectionIndex] ?? paths[0]} />
    </svg>
  );
}

export function AdminNavigation({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();

  // Burada alt ürün rotalarında da ana Ürünler menü öğesini seçili tutuyorum.
  const isCurrentItem = (href: string | undefined) =>
    Boolean(href && (pathname === href || pathname.startsWith(`${href}/`)));

  return (
    <nav aria-label="Ana navigasyon" className="space-y-2.5 px-3 pb-6">
      {navigationSections.map((section, sectionIndex) => (
        <details
          key={section.label}
          className="group/navigation"
          open={section.defaultOpen || section.items.some((item) => isCurrentItem(item.href))}
        >
          <summary className="flex min-h-10 cursor-pointer list-none items-start gap-2.5 rounded-lg px-3 py-2 text-xs font-semibold uppercase tracking-wide text-sidebar-muted hover:bg-sidebar-hover hover:text-sidebar-foreground [&::-webkit-details-marker]:hidden">
            <span className="mt-0.5">
              <NavigationGlyph sectionIndex={sectionIndex} />
            </span>
            <span className="min-w-0 flex-1 whitespace-normal break-words leading-4">
              {section.label}
            </span>
            <svg
              aria-hidden="true"
              viewBox="0 0 20 20"
              className="mt-0.5 size-4 shrink-0 transition-transform group-open/navigation:rotate-180"
            >
              <path
                d="m6 8 4 4 4-4"
                fill="none"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="1.7"
              />
            </svg>
          </summary>

          <ul className="mt-1 space-y-1 pl-2">
            {section.items.map((item) => {
              const isActive = isCurrentItem(item.href);

              return (
                <li key={item.label}>
                  {item.href ? (
                    <Link
                      href={item.href}
                      aria-current={isActive ? "page" : undefined}
                      onClick={onNavigate}
                      className={`flex min-h-10 items-start gap-2.5 rounded-lg border-l-2 px-3 py-2 text-sm font-medium transition-colors ${
                        isActive
                          ? "border-blue-200 bg-sidebar-active text-sidebar-foreground"
                          : "border-transparent text-sidebar-foreground hover:bg-sidebar-hover"
                      }`}
                    >
                      <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-blue-200" aria-hidden="true" />
                      <span className="min-w-0 whitespace-normal break-words leading-5">
                        {item.label}
                      </span>
                    </Link>
                  ) : (
                    <span
                      aria-disabled="true"
                      className="grid min-h-10 grid-cols-[auto_minmax(0,1fr)] items-start gap-x-2.5 rounded-lg border-l-2 border-transparent px-3 py-2 text-sm text-sidebar-muted"
                    >
                      <span className="mt-1.5 size-1.5 shrink-0 rounded-full border border-sidebar-muted" aria-hidden="true" />
                      <span className="min-w-0">
                        <span className="block whitespace-normal break-words leading-5">
                          {item.label}
                        </span>
                        <span className="mt-1 inline-flex rounded-md bg-sidebar-elevated px-1.5 py-0.5 text-xs font-medium text-sidebar-muted">
                          {navigationStatusLabel(item.status)}
                        </span>
                      </span>
                    </span>
                  )}
                </li>
              );
            })}
          </ul>
        </details>
      ))}
    </nav>
  );
}
