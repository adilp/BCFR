-- Fix permissions for memberorg-db user
-- Run this as doadmin user in the memberorg-db database

-- Grant USAGE permission on the schema
GRANT USAGE ON SCHEMA memberorg TO "memberorg-db";

-- Grant CREATE permission on the schema
GRANT CREATE ON SCHEMA memberorg TO "memberorg-db";

-- Grant ALL privileges on ALL existing tables in the schema
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA memberorg TO "memberorg-db";

-- Grant ALL privileges on ALL sequences in the schema
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA memberorg TO "memberorg-db";

-- Grant ALL privileges on ALL functions in the schema
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA memberorg TO "memberorg-db";

-- Set default privileges for future objects created by doadmin
ALTER DEFAULT PRIVILEGES FOR ROLE doadmin IN SCHEMA memberorg 
GRANT ALL ON TABLES TO "memberorg-db";

ALTER DEFAULT PRIVILEGES FOR ROLE doadmin IN SCHEMA memberorg 
GRANT ALL ON SEQUENCES TO "memberorg-db";

ALTER DEFAULT PRIVILEGES FOR ROLE doadmin IN SCHEMA memberorg 
GRANT ALL ON FUNCTIONS TO "memberorg-db";

-- Also set default privileges without specifying the grantor (covers all cases)
ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg 
GRANT ALL ON TABLES TO "memberorg-db";

ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg 
GRANT ALL ON SEQUENCES TO "memberorg-db";

ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg 
GRANT ALL ON FUNCTIONS TO "memberorg-db";

-- Verify permissions (optional - run to check)
SELECT 
    nspname AS schema_name,
    has_schema_privilege('memberorg-db', nspname, 'USAGE') AS has_usage,
    has_schema_privilege('memberorg-db', nspname, 'CREATE') AS has_create
FROM pg_namespace 
WHERE nspname = 'memberorg';

-- Check table permissions
SELECT 
    schemaname,
    tablename,
    has_table_privilege('memberorg-db', schemaname||'.'||tablename, 'SELECT') AS has_select,
    has_table_privilege('memberorg-db', schemaname||'.'||tablename, 'INSERT') AS has_insert,
    has_table_privilege('memberorg-db', schemaname||'.'||tablename, 'UPDATE') AS has_update,
    has_table_privilege('memberorg-db', schemaname||'.'||tablename, 'DELETE') AS has_delete
FROM pg_tables 
WHERE schemaname = 'memberorg';