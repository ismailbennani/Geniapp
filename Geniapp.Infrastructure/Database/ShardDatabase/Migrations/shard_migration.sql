CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Tenants" (
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
);

CREATE TABLE "TenantsData" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "LastWorkerId" uuid,
    "LastModificationDate" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TenantsData" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TenantsData_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_TenantsData_TenantId" ON "TenantsData" ("TenantId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241008180724_Initial', '8.0.8');

COMMIT;

