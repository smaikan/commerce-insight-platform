import { describe, expect, it } from "vitest";
import { buildContactMessageDetailHref, buildContactMessageListHref, hasContactMessageFilters, parseContactMessageListQuery } from "./query";

describe("contact message query", () => {
  it("parses only documented filters and resets invalid values", () => {
    const query = parseContactMessageListQuery({ pageNumber: "3", pageSize: "50", search: " REF ", status: "2", subject: "4", assignedAdminUserId: "u00001", createdFromUtc: "2026-08-01", createdToUtc: "2026-08-21", unknown: "ignored" });
    expect(query).toMatchObject({ pageNumber: 3, pageSize: 50, search: "REF", status: 2, subject: 4, assignedAdminUserId: "U00001", createdFromUtc: "2026-08-01", createdToUtc: "2026-08-21" });
    expect(query.createdFromApiUtc).toBe("2026-08-01T00:00:00.000Z");
    expect(query.createdToApiUtc).toBe("2026-08-21T23:59:59.999Z");
    expect(query).not.toHaveProperty("unknown");
  });

  it("reports inverted date range without sending it to API", () => {
    const query = parseContactMessageListQuery({ createdFromUtc: "2026-09-01", createdToUtc: "2026-08-01" });
    expect(query.dateError).toBeDefined();
    expect(query.createdFromApiUtc).toBeUndefined();
    expect(query.createdToApiUtc).toBeUndefined();
  });

  it("preserves list state in pagination and detail return context", () => {
    const query = parseContactMessageListQuery({ pageNumber: "2", search: "ABC", status: "1" });
    expect(buildContactMessageListHref(query, 4)).toBe("/contact-messages?pageNumber=4&search=ABC&status=1");
    expect(buildContactMessageDetailHref("id/value", query)).toBe("/contact-messages/id%2Fvalue?pageNumber=2&search=ABC&status=1");
    expect(hasContactMessageFilters(query)).toBe(true);
    expect(hasContactMessageFilters(parseContactMessageListQuery({}))).toBe(false);
  });
});
