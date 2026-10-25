WEB_DIR := web
COMPOSE := docker compose -f $(WEB_DIR)/compose.yaml --project-directory $(WEB_DIR)

.PHONY: all setup up down build logs ps re clean fclean webgl webgl-dev db-migrate-dev

all: up

VAULT := Transcendence_obsidian

# Put the vault on its latest main if missing or detached, fast-forward main, leave other branches alone
setup:
	[ -e $(VAULT)/.git ] || git submodule update --init --remote
	git -C $(VAULT) symbolic-ref -q HEAD >/dev/null || git -C $(VAULT) switch -q main
	[ "$$(git -C $(VAULT) branch --show-current)" != main ] || git -C $(VAULT) pull -q --ff-only || echo "warning: could not update $(VAULT)"

up: setup
	$(COMPOSE) up --build -d
	@echo "TRANSCENDENCE is up: https://localhost:4443"
	@echo "ADMINER is up: http://localhost:8180"

down:
	$(COMPOSE) down

build:
	$(COMPOSE) build

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps

# Run Prisma against the Compose database, while keeping generated migration files on the host.
migrate:
	@if [ -z "$(name)" ]; then echo "Usage: make db-migrate-dev name=migration_name"; exit 2; fi
	$(COMPOSE) run --build --rm -v "$(CURDIR)/web/backend/sveltekit/prisma:/app/prisma" migrate npm run db:migrate:dev -- --name "$(name)"
	cd web/backend/sveltekit && npm run db:generate

re: down up

clean:
	$(COMPOSE) down -v

fclean: clean
	$(COMPOSE) down --rmi all

UNITY_PROJECT := UnityProject
GAME_DIR := $(CURDIR)/web/backend/sveltekit/static/public/game

# Release WebGL player served by the site (close the Unity Editor first)
webgl:
	unity build $(UNITY_PROJECT) --profile webgl-release -o $(GAME_DIR)

# Same build with the Development option, served by the site too (never commit it)
webgl-dev:
	unity build $(UNITY_PROJECT) --profile webgl-dev -o $(GAME_DIR)
