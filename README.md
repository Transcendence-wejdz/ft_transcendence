_This project has been created as part of the 42 curriculum by zsonie, erbuffet, jureix-c, dabuchhe, willda-s._

## Description

**Transcendence** is the final project of the 42 common core: a web application built around our own
multiplayer game, a 2D top-down "Vampire Survivors"-like made in Unity. Players create an account on
the website, customize their character (name, class, color, avatar) and play directly in the browser.

The game is designed around three multiplayer modes instead of a single-player loop:

- **4-player co-op** — all players fight the horde together. **Implemented.**
- **2 vs 2** — two teams of survivors fight each other (and the horde). _Designed, not built yet._
- **3 vs 1 asymmetric** — three survivors against one "horde leader" player. _Designed, not built yet._

The repository holds two codebases with very different lifecycles:

- `web/` — the deliverable required by the subject: SvelteKit app, PostgreSQL database, nginx HTTPS
  reverse proxy, all orchestrated with Docker Compose.
- `UnityProject/` — the Unity **source** of the game. It is not part of the web runtime: only its
  exported WebGL **build** is shipped, in `web/backend/sveltekit/static/public/game/`, and served by nginx.

The website and the game share the same player profile: the `/game` page sends the signed-in user's
profile to Unity, and changes made in-game (character, settings, key bindings) are saved back to the
database.

## Instructions

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) with Docker Compose v2.
- `make` (optional, shortcuts below).
- Node.js 22+ and npm, only to work on the web app outside Docker.
- [Unity Hub](https://unity.com/download) with Unity **6000.5.6f1**, only to work on the game.
- Google Chrome (the reference browser for evaluation).

### Cloning

The Obsidian vault (`Transcendence_obsidian/`) is a git submodule:

```sh
git clone --recurse-submodules --remote-submodules git@github.com:Transcendence-wejdz/ft_transcendence.git
```

Already cloned without it? Run `make setup` (`make up` also does it).

### Environment

Secrets live in a gitignored `web/.env`. Create it from the template and change the password:

```sh
cp web/.env.example web/.env
```

| Variable | Purpose |
| --- | --- |
| `POSTGRES_HOST`, `POSTGRES_PORT` | Database host/port inside the Compose network |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Database credentials |
| `ORIGIN` | Public URL of the app (SvelteKit CSRF check) |

### Running

```sh
make up
```

This generates a self-signed certificate on first boot, applies the database migrations, and starts
everything:

- App: **https://localhost:4443** (self-signed certificate, the browser warning is expected)
- Adminer (database UI, local only): http://localhost:8180

| Command | Effect |
| --- | --- |
| `make setup` | Put the vault submodule on its latest `main` |
| `make up` | Build and start the stack in the background |
| `make down` | Stop the stack |
| `make build` | Rebuild images without starting |
| `make logs` | Follow container logs |
| `make ps` | Show container status |
| `make re` | `down` then `up` |
| `make clean` | `down` and remove volumes (wipes the database) |
| `make fclean` | `clean` and remove built images |

### Working on the game

Open `UnityProject/` in Unity Hub with editor **6000.5.6f1**. To ship a change to the website, make a
WebGL build (Gzip compression) into `web/backend/sveltekit/static/public/game/`, then hard-refresh
the browser (Unity caches the build in IndexedDB).

## Team Information

| Login (GitHub) | Role(s) | Responsibilities |
| --- | --- | --- |
| **zsonie** (SaruM4N3) | Product Owner, Developer | Product vision and priorities; Unity game, web ↔ game integration, infrastructure, Obsidian vault |
| **erbuffet** (Nikad0) | Developer | Web application: authentication, sessions, database, profile settings |
| **jureix-c** (E-Berry) | Technical Lead / Architect, Developer | Architecture and technical decisions, code quality |
| **dabuchhe** (dabuchhe) | Scrum Master, Developer | Organization, meetings, task follow-up |
| **willda-s** (willda-s) | Developer | Feature development |

Everyone develops; the roles above add extra responsibilities on top.

## Project Management

- **Meetings**: every 3–4 days on Discord; attendance is not mandatory as long as everyone stays
  reachable. Meeting notes and the project state live in the Obsidian vault.
- **Obsidian vault** (`Transcendence_obsidian/`, separate repository used as a submodule): the team's
  shared brain — one note per code module with its key files and connections. Update the matching
  note whenever you change the code.
- **Branches**:

  ```
  main      protected, PR only, CI green + 1 review — what we present at evaluation
    ▲ merge commit "release: …"
  develop   protected, PR only, CI green — integration of web + game
    ▲ squash merge
  feat/<scope>-<name>   fix/<scope>-<name>   docs/<name>   short-lived, branched from develop
  ```

  Old branches are never deleted without first pushing an `archive/<branch>` tag, so no commit is ever
  lost.
- **Commits**: [Conventional Commits](https://www.conventionalcommits.org/) —
  `type(scope): short summary`, one intention per commit.
  Types: `feat`, `fix`, `refactor`, `chore`, `docs`, `ci`, `build`.
  Scopes: `web`, `api`, `db`, `game`, `net`, `infra`, `ci`, `vault`, `docs`.
  WebGL rebuilds go in their own `chore(game): rebuild WebGL player` commit.
- **Pull requests**: title in Conventional Commit format (it becomes the squash commit on `develop`),
  filled-in [PR template](.github/pull_request_template.md), green CI, at least one review.

## CI/CD

GitHub Actions (`.github/workflows/`):

- **Web CI** (`web-ci.yml`) — on every push/PR to `main`/`develop`: builds the SvelteKit app and its
  Docker image. Required to merge into `main`.
- **Unity Build Check** (`unity-ci.yml`) — on push/PR touching `UnityProject/**`: headless compile check
  via [GameCI](https://game.ci/). A no-op until a Unity license is added as repository secrets (see
  the comment at the top of the workflow).

## Technical Stack

| Layer | Choice | Why |
| --- | --- | --- |
| Frontend + backend | **SvelteKit** (Svelte 5, `adapter-node`, TypeScript) | One framework for pages and API routes, server-side rendering, small runtime |
| Database | **PostgreSQL** | Reliable relational database, JSONB for flexible per-user game settings |
| ORM | **Prisma** | Typed queries and versioned SQL migrations |
| Authentication | **Argon2id** password hashing + database sessions (SHA-256 hashed tokens in an httpOnly cookie) | Current password-hashing recommendation; sessions can be revoked server-side |
| Reverse proxy | **nginx** | HTTPS termination, serves the gzip-compressed Unity build with the right headers |
| Deployment | **Docker Compose** | One command to run certs, nginx, database, migrations, app and Adminer |
| Game | **Unity 6** (WebGL) + Netcode for GameObjects + Unity Relay, Friends, Vivox | Engine the team knows; Relay allows remote multiplayer without opening ports |

## Database Schema

```
users                                         sessions
───────────────────────────────────────       ─────────────────────────────────
id             uuid  PK                ◄──┐   id          text  PK  (sha256 of the token)
email          text  unique               └── user_id     uuid  FK → users.id (cascade)
password_hash  text  (argon2id)               expires_at  timestamptz
display_name   text  unique
color          text  default 'Black'
class          text  default 'Warrior'
avatar_url     text  nullable (data URL)
game_settings  jsonb nullable (in-game options + key bindings)
created_at     timestamptz
```

## Features List

- **Accounts** (erbuffet) — sign up / log in with email or display name, Argon2id hashing, server-side
  validation, rate-limited auth routes, database sessions, logout.
- **Profile settings** (erbuffet) — name, character class, color and avatar picture.
- **Game embedded in the website** (zsonie, erbuffet) — `/game` loads the Unity WebGL build; an account
  is required to play.
- **Shared profile between site and game** (zsonie) — the site sends the profile to Unity; character,
  name, settings and key bindings changed in-game are saved back to the database.
- **4-player online co-op** (zsonie) — host/join with a session code through Unity Relay, lobby with
  ready check, waves of enemies, level-up choices, revive system, game over screen.
- **Friends** (zsonie) — Unity Friends list with invites to your session; your friend name is your
  website display name.
- **Character customization** (zsonie) — 5 classes × 4 colors, each class with its own abilities.
- **Infrastructure** (zsonie) — HTTPS reverse proxy, Docker Compose stack, CI workflows.

## Modules

**Done** (12 points):

| Module | Type | Points | Who |
| --- | --- | --- | --- |
| Web — use a framework for frontend and backend (SvelteKit) | Major | 2 | erbuffet |
| Web — use an ORM for the database (Prisma) | Minor | 1 | erbuffet |
| User management — standard user management and profile | Major | 2 | erbuffet, zsonie |
| Gaming — remote players / multiplayer (online co-op) | Major × 3 | 6 | zsonie |
| Gaming — game customization options | Minor | 1 | zsonie |

**Planned** (from the 13/09 meeting): real-time features and messaging (Web, majors), i18n and
additional browsers (Accessibility, minors), game history / achievements (User management, minor),
AI opponent (AI, major), WAF + secrets vault (Cybersecurity, major), a second game — pig race — with
matchmaking (Gaming, major), advanced chat and gamification (minors), health check (DevOps, minor).
The module list and point totals will be finalized against the subject before evaluation.

## Individual Contributions

### zsonie

Unity game (player, combat and class kits, enemies and waves, procedural terrain, lobby and menus,
co-op networking with Relay, Friends), web ↔ game integration (profile bridge both ways, account
required to play), Docker/nginx infrastructure, CI, Obsidian vault.

### erbuffet

SvelteKit web application: Svelte pages, Unity build integration in the `/game` page, Argon2id
registration/login, database sessions, Prisma + PostgreSQL, profile settings (name, class, color,
avatar).

### jureix-c

<!-- TODO: contributions. -->

### dabuchhe

<!-- TODO: contributions. -->

### willda-s

<!-- TODO: contributions. -->

## Resources

- [SvelteKit docs](https://svelte.dev/docs/kit) · [Svelte 5 docs](https://svelte.dev/docs/svelte)
- [Prisma docs](https://www.prisma.io/docs) · [PostgreSQL docs](https://www.postgresql.org/docs/)
- [Argon2 (RFC 9106)](https://www.rfc-editor.org/rfc/rfc9106)
- [nginx docs](https://nginx.org/en/docs/) · [Docker Compose docs](https://docs.docker.com/compose/)
- [Unity Manual](https://docs.unity3d.com/Manual/index.html) ·
  [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/) ·
  [Unity Relay](https://docs.unity.com/ugs/manual/relay/manual/introduction) ·
  [Unity Friends](https://docs.unity.com/ugs/manual/friends/manual/overview)
- 42 `ft_transcendence` subject (42 intranet)

### AI usage

AI (Claude) is used to write and maintain this README and the Obsidian vault (`Transcendence_obsidian`).
