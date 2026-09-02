.PHONY: preflight run up down clean compose broker broker-aspire keycloak help

# Exported so the docker compose files under tilt/ can resolve their build
# context and volume paths (${FLOTILLA_ROOT}) on `down`/`clean`, when Tilt is
# not the one driving compose.
export FLOTILLA_ROOT := $(CURDIR)

TILT_COMPOSE := -f tilt/docker-compose.broker.yml -f tilt/docker-compose.postgres.yml

preflight: ## Run local (Tilt) environment preflight checks
	uv run --script tilt/preflight.py

run: preflight ## Start the flotilla stack locally via Tilt
	tilt up

up: run ## Alias for run

down: ## Stop the Tilt stack and its containers
	-tilt down 2>/dev/null
	docker compose $(TILT_COMPOSE) down

clean: ## Stop the Tilt stack and remove volumes (DB data)
	-tilt down 2>/dev/null
	docker compose $(TILT_COMPOSE) down -v

compose: ## Run the full stack in Docker
	docker compose up --build

broker: ## Build & run just the MQTT broker (compose)
	docker compose up broker --build

broker-aspire: ## Broker + otel-collector + aspire dashboard (compose)
	docker compose up broker otel-collector aspire-dashboard --build

# Local OpenID Connect issuer, an alternative to Entra ID. See backend/README.md.
keycloak: ## Run a local OIDC issuer instead of Entra ID (compose)
	docker compose --profile keycloak up keycloak

help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-16s\033[0m %s\n", $$1, $$2}'
