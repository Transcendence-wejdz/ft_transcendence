CREATE TABLE "presence_connections" (
    "id" UUID NOT NULL,
    "user_id" UUID NOT NULL,
    "heartbeat_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "presence_connections_pkey" PRIMARY KEY ("id"),
    CONSTRAINT "presence_connections_user_id_fkey"
        FOREIGN KEY ("user_id") REFERENCES "users"("id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX "presence_connections_user_id_heartbeat_at_idx"
    ON "presence_connections"("user_id", "heartbeat_at");
