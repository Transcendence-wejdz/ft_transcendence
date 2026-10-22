-- CreateEnum
CREATE TYPE "Status" AS ENUM ('PENDING', 'ACCEPTED');

-- CreateTable
CREATE TABLE "friendship" (
    "id" UUID NOT NULL DEFAULT gen_random_uuid(),
    "requester_id" UUID NOT NULL,
    "user_Low_id" UUID NOT NULL,
    "user_High_id" UUID NOT NULL,
    "relationStatus" "Status" NOT NULL DEFAULT 'PENDING',
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "friendship_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "friendship_user_Low_id_user_High_id_key" ON "friendship"("user_Low_id", "user_High_id");

-- AddForeignKey
ALTER TABLE "friendship" ADD CONSTRAINT "friendship_requester_id_fkey" FOREIGN KEY ("requester_id") REFERENCES "users"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "friendship" ADD CONSTRAINT "friendship_user_Low_id_fkey" FOREIGN KEY ("user_Low_id") REFERENCES "users"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "friendship" ADD CONSTRAINT "friendship_user_High_id_fkey" FOREIGN KEY ("user_High_id") REFERENCES "users"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
