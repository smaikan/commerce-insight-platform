import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "@/lib/api/problem";
import { INITIAL_ACCOUNT_ACTION_STATE } from "@/modules/account/contracts";

const accountApiMocks = vi.hoisted(() => ({
  createAccountAddress: vi.fn(),
  updateAccountAddress: vi.fn(),
  updateAccountUser: vi.fn(),
  setDefaultAccountAddress: vi.fn(),
  deleteAccountAddress: vi.fn(),
  changeAccountPassword: vi.fn(),
  revokeAccountSession: vi.fn(),
  logoutAllAccountSessions: vi.fn(),
  createAccountReturn: vi.fn(),
}));

vi.mock("@/modules/account/api", () => accountApiMocks);
vi.mock("next/cache", () => ({ revalidatePath: vi.fn() }));
vi.mock("next/navigation", () => ({ redirect: vi.fn() }));
vi.mock("@/lib/auth/cookies", () => ({ clearAuthCookies: vi.fn() }));

import { changePasswordAction, createReturnAction, saveAddressAction } from "@/modules/account/actions";

describe("account actions", () => {
  beforeEach(() => vi.clearAllMocks());

  // Burada API ProblemDetails doğrulama hatasının ilgili adres alanına ve erişilebilir form özetine taşındığını doğruluyorum.
  it("maps API validation errors back to the address form", async () => {
    accountApiMocks.createAccountAddress.mockRejectedValue(new ApiError({
      title: "Validation failed",
      status: 400,
      errors: { Title: ["Adres başlığı çok uzun."] },
    }));

    const formData = new FormData();
    formData.set("type", "0");
    formData.set("title", "Ev");
    formData.set("firstName", "Test");
    formData.set("lastName", "Müşteri");
    formData.set("phoneNumber", "05000000000");
    formData.set("City", "İstanbul");
    formData.set("District", "Kadıköy");
    formData.set("fullAddress", "Test adresi");

    const state = await saveAddressAction(null, INITIAL_ACCOUNT_ACTION_STATE, formData);
    expect(accountApiMocks.createAccountAddress).toHaveBeenCalledOnce();
    expect(state.status).toBe("error");
    expect(state.fieldErrors?.title).toBe("Adres başlığı çok uzun.");
    expect(state.message).toMatch(/işaretli alanları/i);
  });

  // Burada uyuşmayan yeni parola tekrarının API çağrısı yapılmadan ilgili alana bağlandığını doğruluyorum.
  it("rejects mismatched password confirmation before the API request", async () => {
    const formData = new FormData();
    formData.set("currentPassword", "old-password");
    formData.set("newPassword", "new-password");
    formData.set("confirmPassword", "another-password");

    const state = await changePasswordAction(INITIAL_ACCOUNT_ACTION_STATE, formData);
    expect(state.fieldErrors?.confirmPassword).toMatch(/eşleşmiyor/i);
    expect(accountApiMocks.changeAccountPassword).not.toHaveBeenCalled();
  });

  // Burada değişim talebinde replacement seçilmeden API'ye eksik bir payload gönderilmesini engelliyorum.
  it("requires a replacement variant for every selected exchange item", async () => {
    const formData = new FormData();
    const itemId = "2de3f02f-d20a-4e09-8fcb-290870de9ed3";
    formData.set("type", "1");
    formData.append("orderItemId", itemId);
    formData.set(`quantity:${itemId}`, "1");
    const state = await createReturnAction("bb49d4c3-9752-4116-9179-657c8d6259b0", INITIAL_ACCOUNT_ACTION_STATE, formData);
    expect(state.status).toBe("error");
    expect(state.message).toMatch(/yeni varyant/i);
    expect(accountApiMocks.createAccountReturn).not.toHaveBeenCalled();
  });

});
