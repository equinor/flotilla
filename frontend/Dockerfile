FROM node:26-alpine@sha256:dbaa92e5758cbbcf85d65d5403fdb530fe3442cbe8c6dbfb7ef23365450d5070 AS build

RUN corepack enable

WORKDIR /app

COPY . .

RUN pnpm install --frozen-lockfile
RUN pnpm build

FROM nginxinc/nginx-unprivileged:1.31-alpine@sha256:b54ac358b83fc6c965793fd271839b4ea4cdb6e99895bb19618cbc2ca152d972

WORKDIR /app

COPY --from=build /app/dist /app
COPY nginx.conf /etc/nginx/conf.d/default.conf

# The nginx script substitutes the global variables in 'index.html' with the current environment variables when executed
COPY run_nginx.sh run_nginx.sh

EXPOSE 3001

# Switch to admin to change permissions
USER 0
RUN chown -R nginx /etc/nginx/conf.d \
    && chown -R nginx /app \
    && chmod +x run_nginx.sh

# Switch back to unpriviliged
USER 101

CMD /bin/sh -c ". run_nginx.sh"
