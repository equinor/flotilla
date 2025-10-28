broker-aspire:
	docker compose up broker otel-collector aspire-dashboard

compose: 
	docker compose up

broker:
	docker compose up broker
