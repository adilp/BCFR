# Fix PostgreSQL Permission Issues on DigitalOcean

The error "permission denied for schema public" occurs because DigitalOcean's managed PostgreSQL restricts permissions on the public schema for security reasons.

## Solution: Use a Custom Schema

I've updated the `AppDbContext.cs` to use a custom schema called "memberorg" instead of the public schema.

### Steps to Fix:

1. **First, connect as the admin user (doadmin) and create the schema:**

```bash
# Connect to the database
psql "postgresql://doadmin:YOUR_ADMIN_PASSWORD@app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com:25060/defaultdb?sslmode=require"

# Run these SQL commands:
CREATE SCHEMA IF NOT EXISTS memberorg;
GRANT ALL ON SCHEMA memberorg TO "memberorg-db";
ALTER ROLE "memberorg-db" SET search_path TO memberorg, public;
```

Or use the provided script:
```bash
psql "postgresql://doadmin:YOUR_ADMIN_PASSWORD@app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com:25060/defaultdb?sslmode=require" -f ../create-app-schema.sql
```

2. **Remove old migrations and create new ones:**

```bash
cd apps/api

# Remove existing migrations
rm -rf Migrations/

# Create new migration with the custom schema
dotnet ef migrations add InitialCreate

# Update the database
dotnet ef database update
```

3. **For deployment, ensure your connection string uses the memberorg-db user:**

```
DATABASE_URL=postgresql://memberorg-db:YOUR_PASSWORD@app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com:25060/defaultdb?sslmode=require
```

## Alternative: Grant Public Schema Permissions

If you prefer to use the public schema, run this as doadmin:

```sql
GRANT CREATE ON SCHEMA public TO "memberorg-db";
GRANT ALL ON SCHEMA public TO "memberorg-db";
```

However, using a custom schema is recommended for better isolation and security.

## Testing the Connection

After applying the fix, test with:

```bash
dotnet run --environment Production
```

The application should now be able to create tables and run migrations successfully.