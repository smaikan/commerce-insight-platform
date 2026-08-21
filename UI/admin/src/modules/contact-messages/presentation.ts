import type {
  AssignableAdmin,
  ContactMessageActivity,
  ContactMessageDetail,
  ContactMessageReply,
  ContactMessageStatus,
  ContactMessageSubject,
  ContactReplyDeliveryStatus,
} from "@/modules/contact-messages/types";

export const contactMessageStatusOptions: ReadonlyArray<{ value: ContactMessageStatus; label: string }> = [
  { value: 0, label: "Yeni" },
  { value: 1, label: "İşlemde" },
  { value: 2, label: "Müşteri bekleniyor" },
  { value: 3, label: "Çözüldü" },
  { value: 4, label: "Kapalı" },
  { value: 5, label: "Spam" },
];

export const contactMessageSubjectOptions: ReadonlyArray<{ value: ContactMessageSubject; label: string }> = [
  { value: 0, label: "Sipariş desteği" },
  { value: 1, label: "Ürün bilgisi" },
  { value: 2, label: "İade veya iptal desteği" },
  { value: 3, label: "Kurumsal veya toptan" },
  { value: 4, label: "Geri bildirim veya şikâyet" },
  { value: 5, label: "Diğer" },
];

const statusClasses: Record<ContactMessageStatus, string> = {
  0: "border-blue-200 bg-blue-50 text-blue-800",
  1: "border-amber-200 bg-amber-50 text-amber-800",
  2: "border-indigo-200 bg-indigo-50 text-indigo-800",
  3: "border-emerald-200 bg-emerald-50 text-emerald-800",
  4: "border-slate-300 bg-slate-100 text-slate-700",
  5: "border-red-200 bg-red-50 text-red-800",
};

const deliveryLabels: Record<ContactReplyDeliveryStatus, string> = {
  0: "Sırada",
  1: "Gönderildi",
  2: "Yeniden deneniyor",
  3: "Teslim edilemedi",
};

const deliveryClasses: Record<ContactReplyDeliveryStatus, string> = {
  0: "border-blue-200 bg-blue-50 text-blue-800",
  1: "border-emerald-200 bg-emerald-50 text-emerald-800",
  2: "border-amber-200 bg-amber-50 text-amber-800",
  3: "border-red-200 bg-red-50 text-red-800",
};

const statusTransitions: Record<ContactMessageStatus, ContactMessageStatus[]> = {
  0: [1, 2, 4, 5],
  1: [2, 3, 4, 5],
  2: [1, 3, 4, 5],
  3: [1, 4],
  4: [1],
  5: [0, 4],
};

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "Europe/Istanbul",
});
const compactDateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Europe/Istanbul",
});

// Burada numeric iletişim durumunu kullanıcıya açık Türkçe etikete dönüştürüyorum.
export function contactMessageStatusLabel(status: ContactMessageStatus): string {
  return contactMessageStatusOptions.find((option) => option.value === status)?.label ?? "Bilinmiyor";
}

// Burada iletişim durumunu gerçek semantik rozet rolüne eşliyorum.
export function contactMessageStatusClass(status: ContactMessageStatus): string {
  return statusClasses[status];
}

// Burada numeric konu enumunu gelen kutusundaki okunabilir etikete dönüştürüyorum.
export function contactMessageSubjectLabel(subject: ContactMessageSubject): string {
  return contactMessageSubjectOptions.find((option) => option.value === subject)?.label ?? "Bilinmiyor";
}

// Burada yayımlanmış durum matrisine göre yalnız geçerli hedefleri döndürüyorum.
export function contactMessageStatusTransitions(status: ContactMessageStatus): ContactMessageStatus[] {
  return statusTransitions[status];
}

// Burada reply outbox durumunu SMTP sonucunu abartmadan açık etikete dönüştürüyorum.
export function contactReplyDeliveryLabel(status: ContactReplyDeliveryStatus): string {
  return deliveryLabels[status];
}

// Burada reply teslimat durumunu renk dışında metinle de anlaşılır semantik stile bağlıyorum.
export function contactReplyDeliveryClass(status: ContactReplyDeliveryStatus): string {
  return deliveryClasses[status];
}

// Burada API tarihini Türkiye saatiyle, eksik değeri açık tireyle gösteriyorum.
export function formatContactMessageDate(value?: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateTimeFormatter.format(date);
}

// Burada yoğun liste tablosunda tarih ve saati sabit genişliğe yakın numeric biçimde sunuyorum.
export function formatContactMessageCompactDate(value?: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : compactDateTimeFormatter.format(date);
}

// Burada aktif yönetici DTO'sunu seçici ve audit görünümünde kullanılacak güvenli etikete dönüştürüyorum.
export function assignableAdminLabel(admin: Pick<AssignableAdmin, "firstName" | "lastName" | "id">): string {
  const fullName = `${admin.firstName} ${admin.lastName}`.trim();
  return fullName ? `${fullName} · ${admin.id}` : admin.id;
}

// Burada public yönetici kimliğini bilinen aktif yönetici adıyla, bulunamazsa kimliğin kendisiyle gösteriyorum.
export function adminDisplayName(adminUserId: string | null | undefined, admins: readonly AssignableAdmin[]): string {
  if (!adminUserId) return "Atanmamış";
  const admin = admins.find((candidate) => candidate.id === adminUserId);
  return admin ? assignableAdminLabel(admin) : adminUserId;
}

export type ContactActivityEntry = {
  activity: ContactMessageActivity;
  reply?: ContactMessageReply;
  kind: "system" | "status" | "assignment" | "note" | "reply";
};

// Burada API'nin garantili sırasını bozmadan activity kayıtlarını note/reply ayrımı taşıyan sunum girdilerine bağlıyorum.
export function buildContactActivityEntries(detail: Pick<ContactMessageDetail, "activities" | "replies">): ContactActivityEntry[] {
  const repliesById = new Map(detail.replies.map((reply) => [reply.id, reply]));
  return detail.activities.map((activity) => ({
    activity,
    reply: activity.replyId ? repliesById.get(activity.replyId) : undefined,
    kind: activity.type === 3
      ? "note"
      : activity.type === 4
        ? "reply"
        : activity.type === 1
          ? "status"
          : activity.type === 2
            ? "assignment"
            : "system",
  }));
}

// Burada yalnız API'nin doğruladığı sipariş projection'ı için Admin sipariş bağlantısı üretiyorum.
export function verifiedOrderHref(detail: Pick<ContactMessageDetail, "isOrderVerified" | "verifiedOrderId">): string | undefined {
  return detail.isOrderVerified && detail.verifiedOrderId
    ? `/orders/${encodeURIComponent(detail.verifiedOrderId)}`
    : undefined;
}

// Burada filtreli ve filtresiz boş gelen kutusu durumlarını ayrı metinlerle tanımlıyorum.
export function contactMessageEmptyState(filtered: boolean): { title: string; description: string } {
  return filtered
    ? {
        title: "Filtrelere uyan iletişim mesajı bulunamadı",
        description: "Arama veya filtre kriterlerini değiştirerek tekrar deneyin.",
      }
    : {
        title: "Henüz iletişim mesajı bulunmuyor",
        description: "Storefront iletişim formundan gelen mesajlar burada görünecek.",
      };
}
