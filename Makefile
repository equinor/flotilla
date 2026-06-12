.PHONY: broker broker-aspire compose

broker-aspire:
	docker compose up broker otel-collector aspire-dashboard --build

compose:
	docker compose up --build

broker:
	docker compose up broker --build
