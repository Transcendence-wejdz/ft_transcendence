-- AlterTable
ALTER TABLE "game_runs" ADD COLUMN     "checkpoint_kills" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN     "checkpoint_score" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN     "checkpoint_wave" INTEGER NOT NULL DEFAULT 0;
