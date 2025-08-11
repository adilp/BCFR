-- Run this script as the doadmin user to set up the database
-- Connect to your DigitalOcean PostgreSQL database first

-- Create the memberorg schema
CREATE SCHEMA IF NOT EXISTS memberorg;

-- Grant all privileges on the memberorg schema to the app user
GRANT ALL ON SCHEMA memberorg TO "memberorg-db";
GRANT CREATE, USAGE ON SCHEMA memberorg TO "memberorg-db";

-- Set the default search path for the user
ALTER ROLE "memberorg-db" SET search_path TO memberorg, public;

-- Grant privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg GRANT ALL ON TABLES TO "memberorg-db";
ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg GRANT ALL ON SEQUENCES TO "memberorg-db";
ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg GRANT ALL ON FUNCTIONS TO "memberorg-db";

-- Verify the schema was created
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'memberorg';

-- Verify permissions
SELECT 
    nspname AS schema_name,
    rolname AS role_name,
    has_schema_privilege(rolname, nspname, 'CREATE') AS can_create,
    has_schema_privilege(rolname, nspname, 'USAGE') AS can_use
FROM pg_namespace
CROSS JOIN pg_roles
WHERE nspname = 'memberorg' AND rolname = 'memberorg-db';