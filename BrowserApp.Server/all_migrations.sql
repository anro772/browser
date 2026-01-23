CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Username" character varying(50) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "MarketplaceRules" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Site" character varying(500) NOT NULL,
    "Priority" integer NOT NULL,
    "RulesJson" jsonb NOT NULL,
    "AuthorId" uuid NOT NULL,
    "DownloadCount" integer NOT NULL DEFAULT 0,
    "Tags" text[] NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MarketplaceRules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MarketplaceRules_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_MarketplaceRules_AuthorId" ON "MarketplaceRules" ("AuthorId");

CREATE INDEX "IX_MarketplaceRules_CreatedAt" ON "MarketplaceRules" ("CreatedAt");

CREATE INDEX "IX_MarketplaceRules_DownloadCount" ON "MarketplaceRules" ("DownloadCount");

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260118040218_InitialCreate', '8.0.0');

COMMIT;

START TRANSACTION;

CREATE TABLE "Channels" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "OwnerId" uuid NOT NULL,
    "PasswordHash" character varying(64) NOT NULL,
    "IsPublic" boolean NOT NULL DEFAULT TRUE,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "MemberCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Channels" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Channels_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ChannelMembers" (
    "Id" uuid NOT NULL,
    "ChannelId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JoinedAt" timestamp with time zone NOT NULL,
    "LastSyncedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ChannelMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ChannelMembers_Channels_ChannelId" FOREIGN KEY ("ChannelId") REFERENCES "Channels" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ChannelMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ChannelRules" (
    "Id" uuid NOT NULL,
    "ChannelId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Site" character varying(500) NOT NULL,
    "Priority" integer NOT NULL,
    "RulesJson" jsonb NOT NULL,
    "IsEnforced" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ChannelRules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ChannelRules_Channels_ChannelId" FOREIGN KEY ("ChannelId") REFERENCES "Channels" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ChannelMembers_ChannelId" ON "ChannelMembers" ("ChannelId");

CREATE UNIQUE INDEX "IX_ChannelMembers_ChannelId_UserId" ON "ChannelMembers" ("ChannelId", "UserId");

CREATE INDEX "IX_ChannelMembers_UserId" ON "ChannelMembers" ("UserId");

CREATE INDEX "IX_ChannelRules_ChannelId" ON "ChannelRules" ("ChannelId");

CREATE INDEX "IX_Channels_CreatedAt" ON "Channels" ("CreatedAt");

CREATE INDEX "IX_Channels_IsPublic" ON "Channels" ("IsPublic");

CREATE INDEX "IX_Channels_OwnerId" ON "Channels" ("OwnerId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260123051610_AddChannelTables', '8.0.0');

COMMIT;

START TRANSACTION;

CREATE TABLE "ChannelAuditLogs" (
    "Id" uuid NOT NULL,
    "ChannelId" uuid NOT NULL,
    "UserId" uuid,
    "Action" character varying(50) NOT NULL,
    "Metadata" jsonb,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ChannelAuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ChannelAuditLogs_Channels_ChannelId" FOREIGN KEY ("ChannelId") REFERENCES "Channels" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ChannelAuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_ChannelAuditLogs_ChannelId" ON "ChannelAuditLogs" ("ChannelId");

CREATE INDEX "IX_ChannelAuditLogs_Timestamp" ON "ChannelAuditLogs" ("Timestamp");

CREATE INDEX "IX_ChannelAuditLogs_UserId" ON "ChannelAuditLogs" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260123053806_AddChannelAuditLog', '8.0.0');

COMMIT;

