# Manual Database Migration Steps for DigitalOcean

Due to permission restrictions on DigitalOcean's managed PostgreSQL, migrations cannot run automatically. Follow these steps to set up your database:

## One-Time Setup (Run as doadmin)

1. Connect to your database as the admin user:
```bash
psql "postgresql://doadmin:YOUR_ADMIN_PASSWORD@YOUR_HOST:25060/defaultdb?sslmode=require"
```

2. Run the setup script:
```sql
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
```

## Running Migrations

After deploying your application, run migrations manually:

1. From your local machine:
```bash
cd memberorg-app/apps/api
export DATABASE_URL="postgresql://memberorg-db:YOUR_PASSWORD@YOUR_HOST:25060/defaultdb?sslmode=require"
dotnet ef database update
```

2. Or use DigitalOcean App Platform's console:
```bash
# Connect to the running container
doctl apps console YOUR_APP_ID --component api

# Inside the container
cd /app
dotnet ef database update
```

## Verifying the Setup

1. The API health check should work immediately (no database required):
   - https://your-api-url.ondigitalocean.app/api/health

2. Once migrations are applied, the hello endpoint will work:
   - https://your-api-url.ondigitalocean.app/api/hello

## Troubleshooting

If you see "permission denied for database memberorg-db":
- The schema hasn't been created yet
- Run the setup script as doadmin first

If you see "permission denied for schema public":
- You're trying to use the public schema
- Make sure your migrations use the memberorg schema

## Why This Approach?

DigitalOcean's managed PostgreSQL restricts schema creation permissions for security. By:
1. Creating the schema manually as admin
2. Running migrations separately
3. Using a custom schema instead of public

We ensure proper security while maintaining full functionality.