.PHONY: broker broker-aspire compose

broker-aspire:
	docker compose up broker otel-collector aspire-dashboard --build

compose:
	docker compose up --build

broker:
	docker compose up broker --build

# run pretties, eslint and check fo unused exports
format-frontend:
	npx prettier --write src
	npx eslint src
	node_modules/.bin/ts-unused-exports tsconfig.json
	