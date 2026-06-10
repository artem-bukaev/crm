# syntax=docker/dockerfile:1.7

ARG NODE_VERSION=22
ARG NGINX_VERSION=1.27

FROM node:${NODE_VERSION}-alpine AS build
WORKDIR /app

ARG WEBAPP_DIR=src/Crm.WebApp

COPY ${WEBAPP_DIR}/ ./
RUN set -eux; \
    if [ -f pnpm-lock.yaml ]; then \
        corepack enable; \
        pnpm install --frozen-lockfile; \
    elif [ -f yarn.lock ]; then \
        corepack enable; \
        yarn install --frozen-lockfile; \
    elif [ -f package-lock.json ]; then \
        npm ci --no-audit --no-fund; \
    else \
        npm install --no-audit --no-fund; \
    fi

RUN set -eux; \
    if [ -f pnpm-lock.yaml ]; then \
        pnpm run build; \
    elif [ -f yarn.lock ]; then \
        yarn build; \
    else \
        npm run build; \
    fi

FROM nginx:${NGINX_VERSION}-alpine AS runtime

ARG BUILD_OUTPUT=dist

COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/${BUILD_OUTPUT}/ /usr/share/nginx/html/

RUN set -eux; \
    chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/run /etc/nginx/conf.d

USER nginx
EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -q -O - http://127.0.0.1/healthz || exit 1
