import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));
vi.mock("@/lib/api/problem", () => ({ ApiError: class ApiError extends Error {} }));
vi.mock("@/modules/banners/section-config", async () => import("./section-config"));

import {
  getAdminBannerSection,
  getAdminBannerSections,
  updateBannerSection,
} from "./api";
import { BANNER_SECTION_KEYS } from "./section-config";
import type { BannerSection, BannerSectionRequest } from "./types";
import type { AdminSession } from "@/lib/auth/contracts";

const session = { accessToken: "admin-access-token" } as AdminSession;

function section(key: string): BannerSection {
  return { name: key, key, items: [] };
}

describe("banner API istemcisi", () => {
  beforeEach(() => {
    apiRequestMock.mockReset();
    apiRequestMock.mockImplementation((path: string) => Promise.resolve(section(path)));
  });

  // Burada altı yönetim GET isteğinin birbirine karışmadan kendi /admin yoluna gönderildiğini doğruluyorum.
  it("altı bağımsız admin endpointini çağırır", async () => {
    const result = await getAdminBannerSections(session);

    expect(result).toHaveLength(6);
    expect(apiRequestMock.mock.calls.map(([path]) => path)).toEqual([
      "/api/main-banners/admin",
      "/api/alt-banner-1/admin",
      "/api/alt-banner-2/admin",
      "/api/alt-banner-3/admin",
      "/api/alt-banner-4/admin",
      "/api/alt-banner-5/admin",
    ]);
    expect(apiRequestMock.mock.calls.every(([, options]) => options.accessToken === session.accessToken)).toBe(true);
  });

  // Burada tek endpoint hatasının diğer beş bölümün başarılı yükleme sonucunu düşürmediğini doğruluyorum.
  it("bölüm yükleme hatalarını birbirinden yalıtır", async () => {
    apiRequestMock.mockImplementation((path: string) => path === "/api/alt-banner-2/admin"
      ? Promise.reject(new Error("network"))
      : Promise.resolve(section(path)));

    const result = await getAdminBannerSections(session);

    expect(result.filter((item) => item.status === "success")).toHaveLength(5);
    expect(result.find((item) => item.key === "alt-banner-2")).toMatchObject({
      key: "alt-banner-2",
      status: "error",
    });
  });

  // Burada bölüm bazlı PUT işleminin yalnız seçilen public bölüm yoluna ve generated request gövdesine gittiğini doğruluyorum.
  it.each(BANNER_SECTION_KEYS)("%s bölümünü kendi PUT endpointine gönderir", async (key) => {
    const request: BannerSectionRequest = { items: [] };
    await updateBannerSection(key, request, session);

    const expectedPath = key === "main-banner" ? "/api/main-banners" : `/api/${key}`;
    expect(apiRequestMock).toHaveBeenLastCalledWith(expectedPath, {
      method: "PUT",
      body: request,
      accessToken: session.accessToken,
    });
  });

  // Burada tek bölüm okumasının yetkili admin endpointini kullandığını ayrıca sabitliyorum.
  it("tek bölüm yeniden yüklemesini admin yolundan yapar", async () => {
    await getAdminBannerSection("alt-banner-3", session);

    expect(apiRequestMock).toHaveBeenCalledWith("/api/alt-banner-3/admin", {
      accessToken: session.accessToken,
    });
  });
});
