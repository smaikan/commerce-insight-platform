import type { Metadata } from "next";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAssignableAdmins, getContactMessages } from "@/modules/contact-messages/api";
import { ContactMessageFilters } from "@/modules/contact-messages/components/contact-message-filters";
import { ContactMessageLoadProblem } from "@/modules/contact-messages/components/contact-message-load-problem";
import { ContactMessagePagination } from "@/modules/contact-messages/components/contact-message-pagination";
import { ContactMessageTable } from "@/modules/contact-messages/components/contact-message-table";
import { buildContactMessageListHref, parseContactMessageListQuery } from "@/modules/contact-messages/query";

export const metadata: Metadata = { title: "İletişim Mesajları" };

// Burada private gelen kutusunun ilk liste ve atama verisini doğrudan Server Component'te no-store API istemcisiyle okuyorum.
export default async function ContactMessagesPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parseContactMessageListQuery(await searchParams);
  const session = await requireAdminPageSession(buildContactMessageListHref(query));
  const retryHref = buildContactMessageListHref(query);
  let data;
  let loadProblem;
  try {
    data = await Promise.all([getContactMessages(query, session), getAssignableAdmins(session)]);
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    loadProblem = error.problem;
  }
  if (loadProblem) return <ContactMessageLoadProblem problem={loadProblem} retryHref={retryHref} />;
  if (!data) throw new Error("Contact message list could not be resolved.");
  const [page, admins] = data;
  return (
    <div className="w-full">
      <PageHeader title="İletişim Mesajları" description="Storefront iletişim formundan gelen talepleri filtreleyin, atayın ve kayıtlı activity akışı üzerinden yanıtlayın." />
      <section aria-label="İletişim mesajı gelen kutusu" className="overflow-hidden rounded-xl border border-border bg-surface">
        <ContactMessageFilters query={query} admins={admins} />
        <ContactMessageTable page={page} query={query} admins={admins} />
        <ContactMessagePagination page={page} query={query} />
      </section>
    </div>
  );
}
