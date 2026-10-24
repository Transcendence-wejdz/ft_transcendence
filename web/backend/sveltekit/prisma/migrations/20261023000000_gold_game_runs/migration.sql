ALTER TABLE "users" ADD COLUMN "gold" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "users" ADD COLUMN "upgrades" JSONB NOT NULL DEFAULT '{}';

CREATE TABLE IF NOT EXISTS "game_runs" (
    "id" UUID NOT NULL,
    "user_id" UUID NOT NULL,
    "mode" TEXT NOT NULL,
    "started_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "finished_at" TIMESTAMPTZ(6),
    "run_id" INTEGER,
    "score" INTEGER,
    "wave" INTEGER,
    "seconds" INTEGER,
    "kills" INTEGER,
    "victory" BOOLEAN,
    "gold" INTEGER,

    CONSTRAINT "game_runs_pkey" PRIMARY KEY ("id"),
    CONSTRAINT "game_runs_user_id_fkey" FOREIGN KEY ("user_id")
    REFERENCES "users"("id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX IF NOT EXISTS "game_runs_user_id_started_at_idx" ON "game_runs"("user_id", "started_at");
