FROM node:current-slim as build

WORKDIR /app

COPY . .
RUN npm install
RUN npm run build

FROM nginxinc/nginx-unprivileged:1-alpine

WORKDIR /app
COPY --from=build /app/build . 
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 3001

ENTRYPOINT nginx -g "daemon off;"
