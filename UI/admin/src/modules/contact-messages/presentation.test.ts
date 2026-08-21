import { describe, expect, it } from "vitest";
import { buildContactActivityEntries, contactMessageEmptyState, contactMessageStatusClass, contactMessageStatusLabel, contactMessageStatusTransitions, contactReplyDeliveryLabel, verifiedOrderHref } from "./presentation";
import type { ContactMessageActivity, ContactMessageReply } from "./types";

describe("contact message presentation", () => {
  it("maps every numeric status to text, style and documented transitions", () => {
    expect([0, 1, 2, 3, 4, 5].map((status) => contactMessageStatusLabel(status as 0))).toEqual(["Yeni", "İşlemde", "Müşteri bekleniyor", "Çözüldü", "Kapalı", "Spam"]);
    expect(contactMessageStatusClass(0)).toContain("blue");
    expect(contactMessageStatusTransitions(5)).toEqual([0, 4]);
  });

  it("keeps API activity order and distinguishes internal notes from replies", () => {
    const activities = [activity("a", 3, undefined), activity("b", 4, "reply")];
    const replies: ContactMessageReply[] = [{ id: "reply", adminUserId: "U00001", body: "Yanıt", deliveryStatus: 0, createdAt: "2026-08-21T10:01:00Z" }];
    const entries = buildContactActivityEntries({ activities, replies });
    expect(entries.map((entry) => entry.activity.id)).toEqual(["a", "b"]);
    expect(entries.map((entry) => entry.kind)).toEqual(["note", "reply"]);
    expect(entries[1].reply?.body).toBe("Yanıt");
  });

  it("represents all reply delivery states without a retry action", () => {
    expect([0, 1, 2, 3].map((status) => contactReplyDeliveryLabel(status as 0))).toEqual(["Sırada", "Gönderildi", "Yeniden deneniyor", "Teslim edilemedi"]);
  });

  it("links only API-verified order projections", () => {
    expect(verifiedOrderHref({ isOrderVerified: true, verifiedOrderId: "order-id" })).toBe("/orders/order-id");
    expect(verifiedOrderHref({ isOrderVerified: false, verifiedOrderId: "order-id" })).toBeUndefined();
    expect(verifiedOrderHref({ isOrderVerified: true, verifiedOrderId: null })).toBeUndefined();
  });

  it("separates filtered and unfiltered empty states", () => {
    expect(contactMessageEmptyState(false).title).toContain("Henüz");
    expect(contactMessageEmptyState(true).title).toContain("Filtrelere");
  });
});

function activity(id: string, type: 3 | 4, replyId?: string): ContactMessageActivity {
  return { id, type, actorAdminUserId: "U00001", content: type === 3 ? "Not" : null, previousValue: null, newValue: null, replyId: replyId ?? null, createdAt: `2026-08-21T10:0${id === "a" ? "0" : "1"}:00Z` };
}
