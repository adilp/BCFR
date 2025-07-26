# DigitalOcean Database Setup

## The Permission Issue

DigitalOcean's managed PostgreSQL databases restrict creating objects in the `public` schema for security. You need to either:

1. Grant permissions to your database user (recommended)
2. Use a different schema
3. Run migrations manually with admin privileges

## Solution 1: Grant Permissions (Recommended)

Connect to your DigitalOcean database as the `doadmin` user and run:

```sql
-- Grant all privileges on the database to your app user
GRANT ALL PRIVILEGES ON DATABASE your_database_name TO your_app_user;

-- Connect to your database
\c your_database_name

-- Grant schema permissions
GRANT ALL ON SCHEMA public TO your_app_user;
GRANT CREATE ON SCHEMA public TO your_app_user;

-- Grant default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT ALL ON TABLES TO your_app_user;
```

## Solution 2: Use DigitalOcean's Database Dashboard

1. Go to your database in DigitalOcean dashboard
2. Click on "Users & Databases" tab
3. Find your database user
4. Ensure they have "All privileges" on your database

## Solution 3: Run Migrations Manually

If you can't modify permissions, run migrations from your local machine:

```bash
# From apps/api directory
export DATABASE_URL="postgresql://username:password@host:port/database?sslmode=require"
dotnet ef database update
```

## Updated Deployment Workflow

1. **First Deployment Only**:
   - Deploy without automatic migrations
   - Set up database permissions
   - Run migrations manually

2. **Subsequent Deployments**:
   - Automatic migrations will work

## Temporary Fix

To deploy immediately without migrations:

1. Comment out the migration code in Program.cs
2. Deploy the app
3. Run migrations manually
4. Uncomment the code for future deployments

## Connection String Format

DigitalOcean provides DATABASE_URL in this format:
```
postgresql://username:password@host:port/database?sslmode=require
```

Our code automatically parses this in production.