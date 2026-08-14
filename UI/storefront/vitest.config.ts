import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vitest/config";

// Burada test çözümleyicisini Next.js ve TypeScript ile aynı @/src alias'ına bağlayıp bileşen regresyonlarını gerçek import yapısıyla çalıştırıyorum.
export default defineConfig({
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
});
