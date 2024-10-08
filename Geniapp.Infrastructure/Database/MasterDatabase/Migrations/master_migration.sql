CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Shards" (
    "Id" uuid NOT NULL,
    "Name" character varying(512) NOT NULL,
    CONSTRAINT "PK_Shards" PRIMARY KEY ("Id")
);

CREATE TABLE "TenantShardAssociations" (
    "Id" uuid NOT NULL,
    "ShardId" uuid,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_TenantShardAssociations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TenantShardAssociations_Shards_ShardId" FOREIGN KEY ("ShardId") REFERENCES "Shards" ("Id")
);

CREATE INDEX "IX_TenantShardAssociations_ShardId" ON "TenantShardAssociations" ("ShardId");

CREATE UNIQUE INDEX "IX_TenantShardAssociations_TenantId" ON "TenantShardAssociations" ("TenantId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241008175923_Initial', '8.0.8');

COMMIT;

