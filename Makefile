.PHONY: broker broker-aspire compose keycloak

broker-aspire:
	docker compose up broker otel-collector aspire-dashboard --build

compose:
	docker compose up --build

broker:
	docker compose up broker --build

# Local OpenID Connect issuer, an alternative to Entra ID. See backend/README.md.
keycloak:
	docker compose --profile keycloak up keycloak
