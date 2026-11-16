FROM node:26-alpine@sha256:6bd9ab43c0ec0190ca7b2d193906cb8b7edf6e7f4afa43ddeccd6452710bffb5 AS build

RUN npm install --global corepack@0.36.0 && corepack enable

WORKDIR /app

COPY . .

RUN pnpm install --frozen-lockfile
RUN pnpm build

FROM nginxinc/nginx-unprivileged:1.31-alpine@sha256:b5047b9b76845eac124358373c6852377c52e4b05834a47647e955dd91cb8f1e

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
