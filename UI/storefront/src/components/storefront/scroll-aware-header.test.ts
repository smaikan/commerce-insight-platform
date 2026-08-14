import { describe, expect, it } from "vitest";

import { nextHeaderHiddenState } from "./scroll-aware-header";

describe("scroll-aware header visibility", () => {
  // Burada eşik geçildikten sonraki aşağı scroll hareketinin ana header'ı gizlediğini doğruluyorum.
  it("hides after scrolling down beyond the header threshold", () => {
    expect(nextHeaderHiddenState({ previousScrollY: 100, currentScrollY: 130, hidden: false })).toBe(true);
  });

  // Burada sayfa ortasında yukarı yönlü hareket başladığı anda ana header'ın yeniden açıldığını doğruluyorum.
  it("shows again when the user scrolls upward", () => {
    expect(nextHeaderHiddenState({ previousScrollY: 420, currentScrollY: 390, hidden: true })).toBe(false);
  });

  // Burada sayfanın gerçek en üstüne dönüldüğünde görünür durumun kesin olarak korunduğunu doğruluyorum.
  it("stays visible at the top and ignores tiny scroll jitter", () => {
    expect(nextHeaderHiddenState({ previousScrollY: 20, currentScrollY: 0, hidden: true })).toBe(false);
    expect(nextHeaderHiddenState({ previousScrollY: 130, currentScrollY: 134, hidden: true })).toBe(true);
  });
});
