import { beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError } from "@/lib/api/problem";
import { INITIAL_ACCOUNT_ACTION_STATE } from "@/modules/account/contracts";

const accountApiMocks = vi.hoisted(() => ({
  createAccountAddress: vi.fn(),
  updateAccountAddress: vi.fn(),
  updateAccountUser: vi.fn(),
  setDefaultAccountAddress: vi.fn(),
  deleteAccountAddress: vi.fn(),
  cancelAccountOrder: vi.fn(),
}));

vi.mock("@/modules/account/api", () => accountApiMocks);
vi.mock("next/cache", () => ({ revalidatePath: vi.fn() }));

import { saveAddressAction } from "@/modules/account/actions";

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
    formData.set("city", "İstanbul");
    formData.set("district", "Kadıköy");
    formData.set("fullAddress", "Test adresi");

    const state = await saveAddressAction(null, INITIAL_ACCOUNT_ACTION_STATE, formData);
    expect(state.status).toBe("error");
    expect(state.fieldErrors?.title).toBe("Adres başlığı çok uzun.");
    expect(state.message).toMatch(/işaretli alanları/i);
  });
});
