-- AlterTable
ALTER TABLE "users" ADD COLUMN     "unity_player_id" TEXT;

-- CreateIndex
CREATE UNIQUE INDEX "users_unity_player_id_key" ON "users"("unity_player_id");
