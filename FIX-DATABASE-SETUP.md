# Fix Database Setup - Use memberorg-db Instead of defaultdb

## Current Problem
- Your app is configured to use `memberorg-db` database
- But your tables were manually created in `defaultdb` database
- This is why automatic migrations don't work and you get "relation memberorg.Users does not exist" errors

## Solution: Use memberorg-db as Intended

### Step 1: Connect to Database as doadmin
Use TablePlus or psql to connect as `doadmin` user to the `memberorg-db` database:

```
Host: app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com
Port: 25060
Database: memberorg-db
User: doadmin
Password: [Get from DigitalOcean dashboard]
SSL: Required
```

### Step 2: Run Schema Setup
Execute the contents of `setup-memberorg-db.sql`:

```sql
-- Create the memberorg schema in memberorg-db database
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
```

### Step 3: Get memberorg-db User Password
1. Go to DigitalOcean dashboard
2. Navigate to your database cluster
3. Click on "Users & Databases" tab
4. Click on "memberorg-db" user
5. Click "Reset Password" to get a new password
6. Copy this password

### Step 4: Run Migrations
From your local machine, in the memberorg-app directory:

```bash
# Set the DATABASE_URL for memberorg-db (replace YOUR_PASSWORD with actual password)
export DATABASE_URL="postgresql://memberorg-db:YOUR_PASSWORD@app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com:25060/memberorg-db?sslmode=require"

# Navigate to API directory
cd apps/api

# Run migrations
dotnet ef database update -- --environment Production
```

### Step 5: Verify
After migrations complete, check in TablePlus:
1. Connect to `memberorg-db` database
2. Look for `memberorg` schema
3. You should see:
   - `Users` table
   - `Sessions` table
   - `__EFMigrationsHistory` table

### Step 6: Test the App
Your app should now work correctly because:
- It's configured to use `memberorg-db` database
- The tables now exist in `memberorg-db` database
- The schema and permissions are properly set up

## Why This Is Better
1. **Consistent Configuration**: App expects memberorg-db, now uses memberorg-db
2. **Automatic Migrations Work**: Future migrations will run automatically on deploy
3. **Cleaner Setup**: No confusion between defaultdb and memberorg-db
4. **Proper Permissions**: memberorg-db user has full control over its own database

## Optional: Clean Up defaultdb
Once everything works with memberorg-db, you can optionally clean up defaultdb:
1. Connect as doadmin to defaultdb
2. Drop the memberorg schema: `DROP SCHEMA memberorg CASCADE;`
kayzzzzkj
3. This removes the duplicate unused tables from defaultdb
