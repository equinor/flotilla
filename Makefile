.PHONY: preflight run up down clean compose broker broker-aspire keycloak help

# Exported so the docker compose files under tilt/ can resolve their build
# context and volume paths (${FLOTILLA_ROOT}) on `down`/`clean`, when Tilt is
# not the one driving compose.
export FLOTILLA_ROOT := $(CURDIR)

TILT_COMPOSE := -f tilt/docker-compose.broker.yml -f tilt/docker-compose.postgres.yml
TILT_DOWN := tilt down 2>/dev/null || true; docker compose $(TILT_COMPOSE) down

preflight: ## Run local (Tilt) environment preflight checks
	uv run --script tilt/preflight.py

run: preflight ## Start the flotilla stack locally via Tilt
	@cleanup() { \
		status=$$?; \
		trap - 0; \
		trap 'printf "\nCleanup interrupted; run `make down` before restarting.\n" >&2; exit 130' INT TERM; \
		printf "\nStopping Flotilla (press Ctrl+C again to cancel cleanup)...\n"; \
		$(TILT_DOWN); \
		cleanup_status=$$?; \
		trap - INT TERM; \
		if [ "$$status" -ne 0 ]; then exit "$$status"; fi; \
		exit "$$cleanup_status"; \
	}; \
	trap cleanup 0; \
	trap 'exit 130' INT TERM; \
	tilt up

up: run ## Alias for run

down: ## Stop the Tilt stack and its containers
	@$(TILT_DOWN)

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
