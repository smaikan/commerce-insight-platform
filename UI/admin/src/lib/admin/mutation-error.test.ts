import { describe, expect, it } from "vitest";
import { ApiError } from "../api/problem";
import { adminMutationError } from "./mutation-error";

describe("admin mutation hata eşlemesi", () => {
  // Burada bağımlılık çatışmasının teknik detay yerine eyleme dönük alan mesajını kullandığını doğruluyorum.
  it("maps conflict to the domain message", () => {
    const result = adminMutationError(new ApiError({ title: "Conflict", status: 409, traceId: "trace-1" }), "Silinemedi.", "Kayıt kullanımda.");
    expect(result).toEqual({ status: "error", message: "Kayıt kullanımda.", traceId: "trace-1" });
  });

  // Burada sona eren oturumun silme hatası olarak değil yeniden giriş yönlendirmesi olarak anlatıldığını doğruluyorum.
  it("maps unauthorized responses consistently", () => {
    const result = adminMutationError(new ApiError({ title: "Unauthorized", status: 401 }), "Silinemedi.", "Kayıt kullanımda.");
    expect(result.message).toContain("Oturumunuz sona erdi");
  });
});
