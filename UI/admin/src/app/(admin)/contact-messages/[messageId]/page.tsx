import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAssignableAdmins, getContactMessage } from "@/modules/contact-messages/api";
import { ContactMessageDetail } from "@/modules/contact-messages/components/contact-message-detail";
import { ContactMessageLoadProblem } from "@/modules/contact-messages/components/contact-message-load-problem";
import { buildContactMessageDetailHref, buildContactMessageListHref, parseContactMessageListQuery } from "@/modules/contact-messages/query";

export async function generateMetadata({ params }: { params: Promise<{ messageId: string }> }): Promise<Metadata> {
  const { messageId } = await params;
  return { title: `İletişim Mesajı ${messageId}` };
}

// Burada detail'i Server Component'te okuyup 404'ü en yakın not-found sınırına, filtreli dönüşü liste href'ine bağlıyorum.
export default async function ContactMessageDetailPage({ params, searchParams }: { params: Promise<{ messageId: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const { messageId } = await params;
  if (!isUuid(messageId)) notFound();
  const query = parseContactMessageListQuery(await searchParams);
  const returnTo = buildContactMessageDetailHref(messageId, query);
  const session = await requireAdminPageSession(returnTo);
  let data;
  let loadProblem;
  try {
    data = await Promise.all([getContactMessage(messageId, session), getAssignableAdmins(session)]);
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 404) notFound();
    if (!(error instanceof ApiError)) throw error;
    loadProblem = error.problem;
  }
  if (loadProblem) return <ContactMessageLoadProblem problem={loadProblem} retryHref={returnTo} />;
  if (!data) throw new Error("Contact message detail could not be resolved.");
  const [detail, admins] = data;
  return <div className="mx-auto w-full max-w-[1480px]"><PageHeader title={`Mesaj ${detail.referenceNumber}`} description={`${detail.name} · ${detail.email}`} backHref={buildContactMessageListHref(query)} /><ContactMessageDetail detail={detail} admins={admins} /></div>;
}

// Burada API'ye geçersiz kimlik göndermeden route parametresini UUID biçiminde doğruluyorum.
function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
