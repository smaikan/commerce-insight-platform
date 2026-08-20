FROM node:26-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN npm install -g pnpm

FROM base AS builder
# PNPM workspace gereksinimleri için gerekli dosyaları kopyalayalım
WORKDIR /app
# Workspace dosyaları
COPY UI/package.json UI/pnpm-workspace.yaml UI/pnpm-lock.yaml ./
# Sadece admin projesini kopyalıyoruz
COPY UI/admin ./admin
# Bağımlılıkları yüklüyoruz
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --filter ./admin

# Build alıyoruz
WORKDIR /app/admin
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_OPTIONS="--max_old_space_size=4096"
RUN pnpm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3001
ENV HOSTNAME="0.0.0.0"

# Sadece standalone çıktıları kopyalıyoruz
COPY --from=builder /app/admin/public ./admin/public
COPY --from=builder /app/admin/.next/standalone ./
COPY --from=builder /app/admin/.next/static ./admin/.next/static

EXPOSE 3001
CMD ["node", "admin/server.js"]
