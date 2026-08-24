import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { NextRequest } from "next/server";

const { revalidatePathMock, revalidateTagMock } = vi.hoisted(() => ({
  revalidatePathMock: vi.fn(),
  revalidateTagMock: vi.fn(),
}));

vi.mock("next/cache", () => ({
  revalidatePath: revalidatePathMock,
  revalidateTag: revalidateTagMock,
}));

import { POST } from "./route";

const validSecret = "storefront-revalidation-test-secret-32-bytes";

function request(body: unknown, secret = validSecret, url = "http://localhost:3000/api/revalidate") {
  return new NextRequest(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "x-revalidate-secret": secret,
    },
    body: JSON.stringify(body),
  });
}

describe("POST /api/revalidate", () => {
  beforeEach(() => {
    process.env.STOREFRONT_REVALIDATE_SECRET = validSecret;
    revalidatePathMock.mockReset();
    revalidateTagMock.mockReset();
  });

  afterEach(() => {
    delete process.env.STOREFRONT_REVALIDATE_SECRET;
  });

  it("fails closed when the server secret is missing", async () => {
    delete process.env.STOREFRONT_REVALIDATE_SECRET;

    const response = await POST(request({ tag: "banners" }));

    expect(response.status).toBe(503);
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(revalidateTagMock).not.toHaveBeenCalled();
  });

  it("rejects an invalid header secret", async () => {
    const response = await POST(request({ tag: "banners" }, "wrong-secret"));

    expect(response.status).toBe(401);
    expect(revalidateTagMock).not.toHaveBeenCalled();
  });

  it("does not accept the secret from the query string", async () => {
    const url = `http://localhost:3000/api/revalidate?secret=${encodeURIComponent(validSecret)}`;
    const queryOnlyRequest = new NextRequest(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tag: "banners" }),
    });

    const response = await POST(queryOnlyRequest);

    expect(response.status).toBe(401);
    expect(revalidateTagMock).not.toHaveBeenCalled();
  });

  it("revalidates only allowlisted tag and path targets", async () => {
    const response = await POST(request({ tag: "products", path: "/products" }));

    expect(response.status).toBe(200);
    expect(revalidateTagMock).toHaveBeenCalledWith("products", "default");
    expect(revalidatePathMock).toHaveBeenCalledWith("/products", "page");
  });

  it("rejects unsupported targets", async () => {
    const response = await POST(request({ tag: "arbitrary-tag", path: "/account" }));

    expect(response.status).toBe(400);
    expect(revalidateTagMock).not.toHaveBeenCalled();
    expect(revalidatePathMock).not.toHaveBeenCalled();
  });

  it("revalidates the bounded public cache set for an explicit empty object", async () => {
    const response = await POST(request({}));

    expect(response.status).toBe(200);
    expect(revalidateTagMock).toHaveBeenCalledWith("products", "default");
    expect(revalidatePathMock).toHaveBeenCalledWith("/", "layout");
  });

  it("returns a safe failure instead of reporting a false success", async () => {
    revalidateTagMock.mockImplementationOnce(() => {
      throw new Error("sensitive internal failure");
    });

    const response = await POST(request({ tag: "products" }));
    const body = await response.json();

    expect(response.status).toBe(500);
    expect(body).toEqual({ message: "Cache yenileme işlemi tamamlanamadı." });
    expect(JSON.stringify(body)).not.toContain("sensitive internal failure");
  });
});
