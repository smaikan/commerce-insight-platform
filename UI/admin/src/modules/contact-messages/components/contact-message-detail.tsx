import Link from "next/link";
import { ContactMessageControls } from "@/modules/contact-messages/components/contact-message-controls";
import { InternalNoteComposer, ReplyComposer } from "@/modules/contact-messages/components/contact-message-composers";
import {
  adminDisplayName,
  buildContactActivityEntries,
  contactMessageStatusClass,
  contactMessageStatusLabel,
  contactMessageSubjectLabel,
  contactReplyDeliveryClass,
  contactReplyDeliveryLabel,
  formatContactMessageDate,
  verifiedOrderHref,
  type ContactActivityEntry,
} from "@/modules/contact-messages/presentation";
import type { AssignableAdmin, ContactMessageDetail as ContactMessageDetailType } from "@/modules/contact-messages/types";

// Burada mesaj içeriği ve operasyon rail'ini desktop'ta iki kolona, dar ekranda doğal belge sırasına yerleştiriyorum.
export function ContactMessageDetail({ detail, admins }: { detail: ContactMessageDetailType; admins: readonly AssignableAdmin[] }) {
  const snapshot = { concurrencyToken: detail.concurrencyToken, status: detail.status, assignedAdminUserId: detail.assignedAdminUserId, updatedAt: detail.updatedAt };
  const activities = buildContactActivityEntries(detail);
  const orderHref = verifiedOrderHref(detail);
  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_21rem]">
      <main className="order-last min-w-0 space-y-7 xl:order-first">
        <section aria-labelledby="original-message-heading" className="border-b border-border pb-7">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div><p className="font-mono text-xs font-bold text-primary">{detail.referenceNumber}</p><h2 id="original-message-heading" className="mt-1 text-lg font-semibold text-foreground">Orijinal mesaj</h2></div>
            <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-bold ${contactMessageStatusClass(detail.status)}`}>{contactMessageStatusLabel(detail.status)}</span>
          </div>
          <dl className="mt-5 grid gap-4 text-sm sm:grid-cols-2">
            <Info label="Gönderen" value={detail.name} />
            <Info label="E-posta" value={detail.email} breakAll />
          </dl>
          <div className="mt-5 rounded-lg border border-border bg-surface-strong p-4 sm:p-5">
            <p className="whitespace-pre-wrap break-words text-sm leading-7 text-foreground">{detail.message}</p>
          </div>
          <p className="mt-3 rounded-lg border border-warning/30 bg-warning/10 px-3 py-2 text-xs leading-5 text-foreground"><strong>Operasyon uyarısı:</strong> Mesaj şifre, kart veya hassas ödeme verisi içerebilir. Bu içeriği kopyalamayın, loglamayın veya gereksiz kişilere aktarmayın.</p>
        </section>

        <section aria-labelledby="activity-heading">
          <div className="flex flex-wrap items-baseline justify-between gap-2"><h2 id="activity-heading" className="text-lg font-semibold text-foreground">Activity ve yazışmalar</h2><p className="text-xs text-muted">En eskiden yeniye · append-only</p></div>
          {activities.length ? <ol className="mt-5 space-y-4">{activities.map((entry) => <ActivityItem key={entry.activity.id} entry={entry} admins={admins} />)}</ol> : <p className="mt-4 text-sm text-muted">Henüz activity kaydı yok.</p>}
        </section>

        <InternalNoteComposer key={`note-${detail.concurrencyToken}`} messageId={detail.id} initialSnapshot={snapshot} />
        <ReplyComposer messageId={detail.id} />
      </main>

      <aside className="order-first h-fit rounded-xl border border-border bg-surface p-5 xl:order-last xl:sticky xl:top-6" aria-label="Mesaj operasyon bilgileri">
        <ContactMessageControls key={`controls-${detail.concurrencyToken}`} messageId={detail.id} initialSnapshot={snapshot} admins={admins} />
        <dl className="mt-5 space-y-4 border-t border-border pt-5 text-sm">
          <Info label="Konu" value={contactMessageSubjectLabel(detail.subject)} />
          <Info label="Telefon" value={detail.phone || "Girilmedi"} />
          <Info label="Atanan yönetici" value={adminDisplayName(detail.assignedAdminUserId, admins)} />
          <Info label="Girilen sipariş numarası" value={detail.providedOrderNumber || "Girilmedi"} />
          <div><dt className="text-xs font-semibold text-muted">Sipariş doğrulaması</dt><dd className="mt-1 leading-6 text-foreground">{orderHref ? <><span className="block font-semibold text-success">API tarafından doğrulandı</span><Link href={orderHref} className="font-semibold text-primary underline underline-offset-4">Doğrulanmış siparişi aç</Link></> : <span>Doğrulanmadı. Girilen numara yalnız kullanıcı beyanıdır; yetki kanıtı değildir.</span>}</dd></div>
          <Info label="Oluşturulma" value={formatContactMessageDate(detail.createdAt)} />
          <Info label="Son güncelleme" value={formatContactMessageDate(detail.updatedAt)} />
          <Info label="İlk yanıt" value={formatContactMessageDate(detail.firstRespondedAt)} />
          <Info label="Çözülme" value={formatContactMessageDate(detail.resolvedAt)} />
          <Info label="Kapanma" value={formatContactMessageDate(detail.closedAt)} />
          <Info label="Concurrency token" value={detail.concurrencyToken} mono />
          <Info label="Gizlilik bildirimi" value={`${detail.privacyNoticeVersion} · ${formatContactMessageDate(detail.privacyNoticePublishedAt)}`} />
        </dl>
      </aside>
    </div>
  );
}

// Burada audit kaydını note ve müşteri yanıtını görsel/semantik olarak ayıran bir timeline satırına dönüştürüyorum.
function ActivityItem({ entry, admins }: { entry: ContactActivityEntry; admins: readonly AssignableAdmin[] }) {
  const { activity, reply, kind } = entry;
  const title = kind === "note" ? "Dahili not" : kind === "reply" ? "Müşteri yanıtı sıraya alındı" : kind === "status" ? "Durum değiştirildi" : kind === "assignment" ? "Atama değiştirildi" : "Mesaj alındı";
  return (
    <li className={`relative border-l-2 pl-4 ${kind === "note" ? "border-amber-400" : kind === "reply" ? "border-blue-500" : "border-border-strong"}`}>
      <div className="flex flex-wrap items-baseline justify-between gap-2"><h3 className="text-sm font-semibold text-foreground">{title}</h3><time className="whitespace-nowrap text-xs text-muted" dateTime={activity.createdAt}>{formatContactMessageDate(activity.createdAt)}</time></div>
      <p className="mt-1 text-xs text-muted">{activity.actorAdminUserId ? adminDisplayName(activity.actorAdminUserId, admins) : "Sistem"}</p>
      {kind === "status" ? <p className="mt-2 text-sm text-foreground">{activity.previousValue || "—"} → {activity.newValue || "—"}</p> : null}
      {kind === "assignment" ? <p className="mt-2 text-sm text-foreground">{activity.previousValue ? adminDisplayName(activity.previousValue, admins) : "Atanmamış"} → {activity.newValue ? adminDisplayName(activity.newValue, admins) : "Atanmamış"}</p> : null}
      {kind === "note" ? <div className="mt-2 rounded-lg border border-amber-200 bg-amber-50 p-3"><p className="mb-1 text-xs font-bold text-amber-800">Yalnız yöneticiler görür</p><p className="whitespace-pre-wrap break-words text-sm leading-6 text-foreground">{activity.content}</p></div> : null}
      {kind === "reply" && reply ? <div className="mt-2 rounded-lg border border-blue-200 bg-blue-50/50 p-3"><div className="flex flex-wrap items-center justify-between gap-2"><p className="text-xs font-bold text-blue-800">Müşteriye yanıt</p><span className={`rounded-md border px-2 py-0.5 text-xs font-bold ${contactReplyDeliveryClass(reply.deliveryStatus)}`}>{contactReplyDeliveryLabel(reply.deliveryStatus)}</span></div><p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-foreground">{reply.body}</p></div> : null}
      {kind === "reply" && !reply ? <p className="mt-2 text-sm text-danger">İlişkili yanıt kaydı bulunamadı.</p> : null}
    </li>
  );
}

// Burada rail ve üst özet içindeki label/value çiftlerini tutarlı tanım listesi biçiminde sunuyorum.
function Info({ label, value, breakAll = false, mono = false }: { label: string; value: string; breakAll?: boolean; mono?: boolean }) {
  return <div className="min-w-0"><dt className="text-xs font-semibold text-muted">{label}</dt><dd className={`mt-1 text-sm leading-6 text-foreground ${breakAll ? "break-all" : "break-words"} ${mono ? "font-mono text-xs" : ""}`}>{value}</dd></div>;
}
