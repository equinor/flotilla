FROM node:26-alpine@sha256:0b36e8c136b94cd4fcf02188228e76c31ad5872eef3fec8cbd2eee500cfd9e80 AS build

RUN npm install --global corepack@0.36.0 && corepack enable

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
