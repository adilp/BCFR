-- Create schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS memberorg;

-- Create Users table with proper case
CREATE TABLE IF NOT EXISTS memberorg."Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT true
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON memberorg."Users" ("Username");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON memberorg."Users" ("Email");

-- Create Sessions table
CREATE TABLE IF NOT EXISTS memberorg."Sessions" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL REFERENCES memberorg."Users"("Id") ON DELETE CASCADE,
    "Token" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UserAgent" VARCHAR(500),
    "IpAddress" VARCHAR(45)
);

-- Create sessions indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Sessions_Token" ON memberorg."Sessions" ("Token");
CREATE INDEX IF NOT EXISTS "IX_Sessions_UserId" ON memberorg."Sessions" ("UserId");

-- Create migrations history table if it doesn't exist
CREATE TABLE IF NOT EXISTS memberorg."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Mark migrations as applied
INSERT INTO memberorg."__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250726191823_InitialCreate', '9.0.0')
ON CONFLICT DO NOTHING;

INSERT INTO memberorg."__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250809124725_AddAuthentication', '9.0.0')
ON CONFLICT DO NOTHING;