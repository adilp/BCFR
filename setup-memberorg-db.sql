-- Connect to memberorg-db database and set up the schema and tables
-- Run this as doadmin user

-- First, create the memberorg schema in memberorg-db database
CREATE SCHEMA IF NOT EXISTS memberorg;

-- Grant permissions to memberorg-db user
GRANT ALL ON SCHEMA memberorg TO "memberorg-db";
GRANT CREATE ON SCHEMA memberorg TO "memberorg-db";

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg 
GRANT ALL ON TABLES TO "memberorg-db";

ALTER DEFAULT PRIVILEGES IN SCHEMA memberorg 
GRANT ALL ON SEQUENCES TO "memberorg-db";

-- Set the search path for memberorg-db user
ALTER USER "memberorg-db" SET search_path TO memberorg, public;