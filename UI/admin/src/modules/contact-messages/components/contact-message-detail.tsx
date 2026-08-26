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

// Burada mesaj detay görünümünü, aktivite zaman çizelgesini ve operasyon yönetimini derli toplu ve modern bir hiyerarşide sunuyorum.
export function ContactMessageDetail({
  detail,
  admins,
}: {
  detail: ContactMessageDetailType;
  admins: readonly AssignableAdmin[];
}) {
  const snapshot = {
    concurrencyToken: detail.concurrencyToken,
    status: detail.status,
    assignedAdminUserId: detail.assignedAdminUserId,
    updatedAt: detail.updatedAt,
  };
  const activities = buildContactActivityEntries(detail);
  const orderHref = verifiedOrderHref(detail);

  const senderInitial = detail.name?.trim().charAt(0).toUpperCase() || "M";

  return (
    <div className="space-y-5">
      {/* 1. Üst Özet Şeridi */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-surface p-4 shadow-2xs">
        <div className="flex flex-wrap items-center gap-2.5">
          <span className="inline-flex items-center rounded-md border border-primary/25 bg-primary-soft/60 px-2.5 py-1 font-mono text-xs font-bold text-primary whitespace-nowrap">
            #{detail.referenceNumber}
          </span>
          <span
            className={`inline-flex items-center rounded-md border px-2.5 py-1 text-xs font-semibold whitespace-nowrap ${contactMessageStatusClass(
              detail.status,
            )}`}
          >
            {contactMessageStatusLabel(detail.status)}
          </span>
          <span className="inline-flex items-center rounded-md border border-border bg-surface-subtle px-2.5 py-1 text-xs font-medium text-foreground whitespace-nowrap">
            {contactMessageSubjectLabel(detail.subject)}
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-4 text-xs text-muted">
          <span className="whitespace-nowrap">
            Gönderen: <strong className="font-semibold text-foreground">{detail.name}</strong>
          </span>
          <span className="whitespace-nowrap">
            Tarih: <span className="font-mono tabular-nums">{formatContactMessageDate(detail.createdAt)}</span>
          </span>
        </div>
      </div>

      {/* 2. Ana 2 Kolonlu Düzen */}
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px] items-start">
        {/* Sol / Ana Kolon: Mesaj, Geçmiş ve Formlar */}
        <main className="min-w-0 space-y-6">
          {/* Orijinal Mesaj Kartı */}
          <section
            aria-labelledby="original-message-heading"
            className="rounded-xl border border-border bg-surface p-5 sm:p-6 shadow-2xs"
          >
            {/* Gönderen Bilgisi Başlığı */}
            <div className="flex items-start justify-between gap-4 border-b border-border/70 pb-4">
              <div className="flex items-center gap-3">
                <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary-soft text-sm font-bold text-primary">
                  {senderInitial}
                </div>
                <div className="min-w-0">
                  <h2 id="original-message-heading" className="truncate font-semibold text-sm text-foreground">
                    {detail.name}
                  </h2>
                  <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-muted">
                    <span className="truncate">{detail.email}</span>
                    {detail.phone ? <span className="whitespace-nowrap">· {detail.phone}</span> : null}
                  </div>
                </div>
              </div>
              <time className="shrink-0 font-mono text-xs tabular-nums text-muted" dateTime={detail.createdAt}>
                {formatContactMessageDate(detail.createdAt)}
              </time>
            </div>

            {/* Mesaj Gövdesi */}
            <div className="mt-4 rounded-xl border border-border/80 bg-surface-subtle/40 p-4 sm:p-5">
              <p className="whitespace-pre-wrap break-words text-sm leading-relaxed text-foreground font-normal">
                {detail.message}
              </p>
            </div>

            {/* İlişkili Sipariş Bilgisi Varsa */}
            {detail.providedOrderNumber ? (
              <div className="mt-3.5 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border bg-surface-strong p-3 text-xs">
                <div className="flex items-center gap-2">
                  <span className="text-muted">Girilen Sipariş:</span>
                  <span className="font-mono font-bold text-foreground whitespace-nowrap">
                    {detail.providedOrderNumber}
                  </span>
                </div>
                {orderHref ? (
                  <Link
                    href={orderHref}
                    className="inline-flex items-center gap-1 font-semibold text-success hover:underline whitespace-nowrap"
                  >
                    <svg aria-hidden="true" viewBox="0 0 20 20" fill="currentColor" className="size-3.5">
                      <path
                        fillRule="evenodd"
                        d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z"
                        clipRule="evenodd"
                      />
                    </svg>
                    <span>API Doğruladı · Siparişi Aç →</span>
                  </Link>
                ) : (
                  <span className="italic text-muted">Kullanıcı Beyanı (Doğrulanmadı)</span>
                )}
              </div>
            ) : null}

            {/* Operasyon Güvenlik Uyarısı */}
            <div className="mt-3.5 flex items-start gap-2 rounded-lg border border-amber-300/60 bg-amber-50/80 px-3.5 py-2.5 text-xs leading-5 text-amber-950">
              <svg aria-hidden="true" viewBox="0 0 20 20" fill="currentColor" className="mt-0.5 size-4 shrink-0 text-amber-700">
                <path
                  fillRule="evenodd"
                  d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495ZM10 5a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 5Zm0 9a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z"
                  clipRule="evenodd"
                />
              </svg>
              <div>
                <strong>Operasyon Uyarısı:</strong> Mesaj şifre, kart veya hassas ödeme verisi içerebilir. Bu içeriği harici ortamlara kopyalamayın.
              </div>
            </div>
          </section>

          {/* Activity ve İletişim Geçmişi (Timeline) */}
          <section
            aria-labelledby="activity-heading"
            className="rounded-xl border border-border bg-surface p-5 sm:p-6 shadow-2xs"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2 border-b border-border/70 pb-3">
              <h2 id="activity-heading" className="text-xs font-bold uppercase tracking-wider text-muted">
                İşlem ve Yazışma Geçmişi ({activities.length})
              </h2>
              <p className="text-xs text-muted">Kronolojik Akış</p>
            </div>

            {activities.length ? (
              <ol className="mt-4 space-y-3.5">
                {activities.map((entry) => (
                  <ActivityItem key={entry.activity.id} entry={entry} admins={admins} />
                ))}
              </ol>
            ) : (
              <p className="mt-4 text-xs text-muted">Henüz activity kaydı yok.</p>
            )}
          </section>

          {/* Dahili Not ve Müşteri Yanıtı Çalışma Alanı */}
          <div className="space-y-4">
            <div className="rounded-xl border border-border bg-surface p-5 sm:p-6 shadow-2xs">
              <InternalNoteComposer key={`note-${detail.concurrencyToken}`} messageId={detail.id} initialSnapshot={snapshot} />
            </div>

            <div className="rounded-xl border border-border bg-surface p-5 sm:p-6 shadow-2xs">
              <ReplyComposer messageId={detail.id} />
            </div>
          </div>
        </main>

        {/* Sağ Kolon: Durum & Atama Kontrolleri + Detay Kartı */}
        <aside className="space-y-5 xl:sticky xl:top-6" aria-label="Mesaj operasyon bilgileri">
          {/* 1. Operasyon Yönetimi */}
          <div className="rounded-xl border border-border bg-surface p-5 shadow-2xs">
            <h2 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted">
              Operasyon Yönetimi
            </h2>
            <ContactMessageControls
              key={`controls-${detail.concurrencyToken}`}
              messageId={detail.id}
              initialSnapshot={snapshot}
              admins={admins}
            />
          </div>

          {/* 2. Mesaj Meta Bilgileri */}
          <div className="rounded-xl border border-border bg-surface p-5 shadow-2xs">
            <h2 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted">
              Detay Bilgileri
            </h2>

            <dl className="space-y-3 divide-y divide-border/60 text-xs">
              <InfoRow label="Konu" value={contactMessageSubjectLabel(detail.subject)} />
              <InfoRow label="Gönderen Adı" value={detail.name} />
              <InfoRow label="E-posta" value={detail.email} breakAll />
              <InfoRow label="Telefon" value={detail.phone || "—"} />
              <InfoRow
                label="Atanan Yönetici"
                value={adminDisplayName(detail.assignedAdminUserId, admins)}
                nowrap
              />
              <InfoRow
                label="Sipariş Numarası"
                value={detail.providedOrderNumber || "—"}
                mono
                nowrap
              />
              <InfoRow
                label="Oluşturulma"
                value={formatContactMessageDate(detail.createdAt)}
                mono
                nowrap
              />
              <InfoRow
                label="Son Güncelleme"
                value={formatContactMessageDate(detail.updatedAt)}
                mono
                nowrap
              />
              <InfoRow
                label="İlk Yanıt"
                value={formatContactMessageDate(detail.firstRespondedAt)}
                mono
                nowrap
              />
              <InfoRow
                label="Çözülme"
                value={formatContactMessageDate(detail.resolvedAt)}
                mono
                nowrap
              />
              <InfoRow
                label="Kapanma"
                value={formatContactMessageDate(detail.closedAt)}
                mono
                nowrap
              />
              <div className="pt-3">
                <dt className="text-muted font-medium mb-1">Concurrency Token</dt>
                <dd className="font-mono text-[11px] bg-surface-subtle px-2 py-1 rounded border border-border text-foreground break-all">
                  {detail.concurrencyToken}
                </dd>
              </div>
              <InfoRow
                label="Gizlilik Bildirimi"
                value={`${detail.privacyNoticeVersion} · ${formatContactMessageDate(detail.privacyNoticePublishedAt)}`}
              />
            </dl>
          </div>
        </aside>
      </div>
    </div>
  );
}

// Burada audit kaydını not ve yanıt tipine göre temiz ve renkli bir kart timeline elemanına dönüştürüyorum.
function ActivityItem({
  entry,
  admins,
}: {
  entry: ContactActivityEntry;
  admins: readonly AssignableAdmin[];
}) {
  const { activity, reply, kind } = entry;
  const title =
    kind === "note"
      ? "Dahili Not"
      : kind === "reply"
        ? "Müşteriye Yanıt Sıraya Alındı"
        : kind === "status"
          ? "Durum Güncellendi"
          : kind === "assignment"
            ? "Atama Değiştirildi"
            : "Mesaj Alındı";

  return (
    <li
      className={`relative rounded-lg border p-3.5 transition-colors ${
        kind === "note"
          ? "border-amber-200 bg-amber-50/60"
          : kind === "reply"
            ? "border-blue-200 bg-blue-50/40"
            : "border-border/80 bg-surface-subtle/40"
      }`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/40 pb-2">
        <div className="flex items-center gap-2">
          <span
            className={`inline-block size-2 rounded-full ${
              kind === "note"
                ? "bg-amber-500"
                : kind === "reply"
                  ? "bg-blue-600"
                  : "bg-muted"
            }`}
          />
          <h3 className="text-xs font-bold text-foreground">{title}</h3>
          {kind === "reply" && reply ? (
            <span
              className={`rounded-md border px-1.5 py-0.5 text-[10px] font-bold whitespace-nowrap ${contactReplyDeliveryClass(
                reply.deliveryStatus,
              )}`}
            >
              {contactReplyDeliveryLabel(reply.deliveryStatus)}
            </span>
          ) : null}
        </div>

        <time className="whitespace-nowrap font-mono text-[11px] tabular-nums text-muted" dateTime={activity.createdAt}>
          {formatContactMessageDate(activity.createdAt)}
        </time>
      </div>

      <div className="mt-2 text-xs">
        <p className="text-muted">
          İşlemi Yapan:{" "}
          <strong className="font-semibold text-foreground">
            {activity.actorAdminUserId ? adminDisplayName(activity.actorAdminUserId, admins) : "Sistem"}
          </strong>
        </p>

        {kind === "status" ? (
          <p className="mt-1.5 font-medium text-foreground">
            {activity.previousValue || "—"} → <span className="font-bold">{activity.newValue || "—"}</span>
          </p>
        ) : null}

        {kind === "assignment" ? (
          <p className="mt-1.5 font-medium text-foreground">
            {activity.previousValue ? adminDisplayName(activity.previousValue, admins) : "Atanmamış"} →{" "}
            <span className="font-bold">
              {activity.newValue ? adminDisplayName(activity.newValue, admins) : "Atanmamış"}
            </span>
          </p>
        ) : null}

        {kind === "note" ? (
          <div className="mt-2 rounded-md bg-surface-strong p-2.5 border border-amber-200/80">
            <p className="whitespace-pre-wrap break-words text-xs leading-relaxed text-foreground">{activity.content}</p>
          </div>
        ) : null}

        {kind === "reply" && reply ? (
          <div className="mt-2 rounded-md bg-surface-strong p-2.5 border border-blue-200/80">
            <p className="whitespace-pre-wrap break-words text-xs leading-relaxed text-foreground">{reply.body}</p>
          </div>
        ) : null}

        {kind === "reply" && !reply ? (
          <p className="mt-2 font-semibold text-danger">İlişkili yanıt kaydı bulunamadı.</p>
        ) : null}
      </div>
    </li>
  );
}

// Burada sağ paneldeki key-value satırlarını derli toplu formatta sunuyorum.
function InfoRow({
  label,
  value,
  breakAll = false,
  mono = false,
  nowrap = false,
}: {
  label: string;
  value: string;
  breakAll?: boolean;
  mono?: boolean;
  nowrap?: boolean;
}) {
  return (
    <div className="flex items-baseline justify-between gap-2 pt-2.5 first:pt-0">
      <dt className="text-muted shrink-0">{label}</dt>
      <dd
        className={`text-right font-medium text-foreground ${
          nowrap ? "whitespace-nowrap" : breakAll ? "break-all" : "break-words"
        } ${mono ? "font-mono tabular-nums" : ""}`}
      >
        {value}
      </dd>
    </div>
  );
}
