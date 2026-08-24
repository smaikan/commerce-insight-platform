import { describe, expect, it } from "vitest";
import { buildCurrentAccountListHref, buildCurrentAccountStatementHref, canonicalPageNumber, parseCurrentAccountListQuery, parseCurrentAccountStatementQuery } from "./query";

describe("current account list query", () => {
  it("keeps only documented pagination parameters", () => {
    expect(parseCurrentAccountListQuery({ search: "x", type: "2", pageNumber: "3", pageSize: "50" })).toEqual({ pageNumber: 3, pageSize: 50 });
  });
  it("bounds invalid values and builds canonical links", () => {
    expect(parseCurrentAccountListQuery({ pageNumber: "0", pageSize: "101" })).toEqual({ pageNumber: 1, pageSize: 20 });
    expect(buildCurrentAccountListHref({ pageNumber: 1, pageSize: 20 })).toBe("/accounting/current-accounts");
    expect(buildCurrentAccountListHref({ pageNumber: 2, pageSize: 50 })).toBe("/accounting/current-accounts?pageNumber=2&pageSize=50");
  });
});

describe("current account statement query", () => {
  it("uses namespaced, bounded pagination parameters", () => {
    expect(parseCurrentAccountStatementQuery({ pageNumber: "9", statementPageNumber: "2", statementPageSize: "50" })).toEqual({ statementPageNumber: 2, statementPageSize: 50 });
    expect(parseCurrentAccountStatementQuery({ statementPageNumber: "0", statementPageSize: "101" })).toEqual({ statementPageNumber: 1, statementPageSize: 20 });
  });

  it("builds encoded canonical statement links", () => {
    const query = { statementPageNumber: 1, statementPageSize: 20 };
    expect(buildCurrentAccountStatementHref("a/b", query)).toBe("/accounting/current-accounts/a%2Fb");
    expect(buildCurrentAccountStatementHref("id", query, 3)).toBe("/accounting/current-accounts/id?statementPageNumber=3");
  });

  it("canonicalizes pages beyond the current result set", () => {
    expect(canonicalPageNumber(9, 3)).toBe(3);
    expect(canonicalPageNumber(2, 0)).toBe(1);
    expect(canonicalPageNumber(3, 3)).toBeNull();
  });
});
