"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { AccountIcon } from "@/modules/account/components/account-icon";
import { ACCOUNT_DESTINATIONS } from "@/modules/account/navigation";

// Burada hesap sayfalarında aktif hedefi belirginleştiren, mobilde yatay ve masaüstünde dikey çalışan yerel navigasyonu sunuyorum.
export function AccountSidebar() {
  const pathname = usePathname();

  return (
    <nav aria-label="Hesap navigasyonu" className="max-w-full overflow-x-auto overscroll-x-contain lg:overflow-visible">
      <ul className="flex w-max gap-1 border-b border-line pb-3 lg:w-full lg:flex-col lg:border-b-0 lg:pb-0">
        {ACCOUNT_DESTINATIONS.map((item) => {
          const current = item.href === "/account" ? pathname === item.href : pathname.startsWith(item.href);
          return (
            <li key={item.href}>
              <Link
                href={item.href}
                prefetch={false}
                aria-current={current ? "page" : undefined}
                className={`focus-ring flex min-h-12 items-center gap-3 px-3 py-2 text-sm font-bold transition-colors ${
                  current ? "bg-brand-950 text-white" : "text-ink hover:bg-surface-subtle hover:text-brand-700"
                }`}
              >
                <AccountIcon icon={item.icon} className="size-4.5 shrink-0" />
                {item.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
