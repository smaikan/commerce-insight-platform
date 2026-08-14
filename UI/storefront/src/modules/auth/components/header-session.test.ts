import { afterEach, describe, expect, it, vi } from "vitest";

import { loadHeaderSessionState } from "./header-session";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("loadHeaderSessionState", () => {
  it("eş zamanlı çağrılarda tek oturum isteğini paylaşır", async () => {
    let resolveFetch: ((response: Response) => void) | undefined;
    const fetchPromise = new Promise<Response>((resolve) => {
      resolveFetch = resolve;
    });
    const fetchMock = vi.fn(() => fetchPromise);
    vi.stubGlobal("fetch", fetchMock);

    // Burada Strict Mode benzeri iki eş zamanlı çağrının yalnız bir ağ isteği oluşturduğunu doğruluyorum.
    const firstRequest = loadHeaderSessionState();
    const secondRequest = loadHeaderSessionState();

    expect(fetchMock).toHaveBeenCalledTimes(1);
    resolveFetch?.({
      ok: true,
      json: async () => ({ authenticated: true }),
    } as Response);

    await expect(Promise.all([firstRequest, secondRequest])).resolves.toEqual([
      "authenticated",
      "authenticated",
    ]);
  });

  it("başarısız oturum isteğini misafir durumu olarak çözer", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("network error")));

    // Burada bağlantı hatasının reddedilmiş promise olarak dışarı taşmayıp güvenli misafir durumuna dönüştüğünü doğruluyorum.
    await expect(loadHeaderSessionState()).resolves.toBe("guest");
  });
});
