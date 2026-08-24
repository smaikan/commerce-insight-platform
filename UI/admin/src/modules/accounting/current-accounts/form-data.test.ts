import { describe, expect, it } from "vitest";
import { isCanonicalUserPublicId, parseCurrentAccountForm } from "./form-data";

function validForm(): FormData {
  const form = new FormData();
  form.set("code", "  cr-001  ");
  form.set("name", "  Örnek Cari  ");
  form.set("type", "3");
  form.set("email", "accounting@example.com");
  form.set("userId", "UABC123");
  return form;
}

describe("current account form data", () => {
  it("normalizes whitespace and maps empty optional values to null", () => {
    const result = parseCurrentAccountForm(validForm());
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.input).toMatchObject({ code: "cr-001", name: "Örnek Cari", type: 3, tradeName: null, email: "accounting@example.com", userId: "UABC123" });
  });

  it("rejects missing identity, invalid type, email and public user id", () => {
    const form = validForm();
    form.set("code", "");
    form.set("name", "");
    form.set("type", "9");
    form.set("email", "invalid");
    form.set("userId", "customer-1");
    const result = parseCurrentAccountForm(form);
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(Object.keys(result.state.fieldErrors ?? {})).toEqual(expect.arrayContaining(["code", "name", "type", "email", "userId"]));
  });

  it("enforces the documented field length limits", () => {
    const form = validForm();
    form.set("addressLine", "a".repeat(501));
    const result = parseCurrentAccountForm(form);
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.state.fieldErrors?.addressLine).toEqual(["Adres en fazla 500 karakter olabilir."]);
  });

  it("accepts only canonical non-zero public user IDs", () => {
    expect(isCanonicalUserPublicId("U00001")).toBe(true);
    expect(isCanonicalUserPublicId("UABC123")).toBe(true);
    expect(isCanonicalUserPublicId("U00000")).toBe(false);
    expect(isCanonicalUserPublicId("U000001")).toBe(false);
    expect(isCanonicalUserPublicId("u00001")).toBe(false);
  });

  it("returns the submitted draft with validation errors", () => {
    const form = validForm();
    form.set("name", "");
    form.set("city", "  İstanbul  ");
    const result = parseCurrentAccountForm(form);
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.state.draft).toMatchObject({ code: "cr-001", name: "", city: "İstanbul", type: "3" });
  });
});
