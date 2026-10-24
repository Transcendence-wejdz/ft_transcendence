WEB_DIR := web
COMPOSE := docker compose -f $(WEB_DIR)/compose.yaml --project-directory $(WEB_DIR)

.PHONY: all setup up down build logs ps re clean fclean webgl webgl-dev

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

re: down up

clean:
	$(COMPOSE) down -v

fclean: clean
	$(COMPOSE) down --rmi all

UNITY_PROJECT := UnityProject
GAME_DIR := $(CURDIR)/web/backend/sveltekit/static/public/game
GAME_DEV_DIR := $(CURDIR)/$(UNITY_PROJECT)/Builds/WebGL-dev

# Release WebGL player served by the site (close the Unity Editor first)
webgl:
	unity build $(UNITY_PROJECT) --profile webgl-release -o $(GAME_DIR)
	rm -rf $(GAME_DIR)/*_BurstDebugInformation_DoNotShip

# Same build with the Development option, kept out of the site (gitignored)
webgl-dev:
	unity build $(UNITY_PROJECT) --profile webgl-dev -o $(GAME_DEV_DIR)
