FROM node:current-slim as build

WORKDIR /app

COPY . .
RUN npm install
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.23-alpine

WORKDIR /app
COPY --from=build /app/build . 

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY run_nginx.sh run_nginx.sh

EXPOSE 3001

USER 0
RUN chown -R nginx /etc/nginx/conf.d \
    && chown -R nginx /app \
    && chmod +x run_nginx.sh
USER 101
CMD /bin/sh -c ". run_nginx.sh"
