FROM node:26-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN npm install -g pnpm

FROM base AS builder
# PNPM workspace gereksinimleri
WORKDIR /app
COPY UI/package.json UI/pnpm-workspace.yaml UI/pnpm-lock.yaml ./
# Klasör yapısını pnpm-workspace.yaml (packages: - "storefront") dosyasına uyduruyoruz
COPY UI/storefront ./storefront
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --filter ./storefront

# Build alıyoruz
WORKDIR /app/storefront
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_OPTIONS="--max_old_space_size=4096"
RUN pnpm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Monorepo ortamında next standalone klasör yapısı /app/storefront şeklinde çıkar.
COPY --from=builder /app/storefront/public ./storefront/public
COPY --from=builder /app/storefront/.next/standalone ./
COPY --from=builder /app/storefront/.next/static ./storefront/.next/static

EXPOSE 3000
CMD ["node", "storefront/server.js"]
