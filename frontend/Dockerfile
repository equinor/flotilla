FROM node:lts-slim as build

WORKDIR /app

COPY package.json package-lock.json tsconfig.json ./
RUN npm install
RUN mkdir -p node_modules/.cache && chmod -R 777 node_modules/.cache
COPY . ./

FROM node:lts-slim
WORKDIR /app
COPY --from=build /app ./
EXPOSE 3001

USER 1001

ENTRYPOINT npm start
