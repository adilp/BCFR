# Database Migration Workflow

## Initial Setup

1. **Start Local PostgreSQL 15 (matches DigitalOcean)**
```bash
docker-compose -f docker-compose.local.yml up -d
```

2. **Install EF Core Tools (if not already installed)**
```bash
dotnet tool install --global dotnet-ef
```

## Creating Your First Migration

```bash
cd apps/api

# Restore packages
dotnet restore

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply to local database
dotnet ef database update
```

## Migration Workflow

### 1. Local Development
- Make changes to your models
- Create migration: `dotnet ef migrations add YourMigrationName`
- Test locally: `dotnet ef database update`
- Verify everything works

### 2. Deploy to DigitalOcean
- Commit migration files (in Migrations folder)
- Push to GitHub: `git push`
- DigitalOcean automatically:
  - Rebuilds your app
  - Runs migrations on startup (configured in Program.cs)

## Important Files

- **Migrations/** - Auto-generated migration files (commit these!)
- **Data/AppDbContext.cs** - Your database context
- **Models/** - Your entity models
- **appsettings.Development.json** - Local connection string
- **Program.cs** - Handles production DATABASE_URL

## Connection Details

### Local (Docker)
- Host: localhost
- Port: 5433
- Database: memberorg_local
- User: local_user
- Password: local_password

### Production (DigitalOcean)
- Automatically configured via DATABASE_URL environment variable
- Migrations run automatically on deployment

## Common Commands

```bash
# View pending migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Update database to specific migration
dotnet ef database update MigrationName

# Drop database (local only!)
dotnet ef database drop
```

## Troubleshooting

1. **Can't connect to database**
   - Ensure Docker is running: `docker ps`
   - Check connection string in appsettings.Development.json

2. **Migration fails on DigitalOcean**
   - Check build logs in DigitalOcean dashboard
   - Ensure DATABASE_URL is set correctly

3. **Model changes not reflected**
   - Create a new migration
   - Don't edit existing migrations

## Safety Notes

- Migrations are applied automatically in production
- Always test migrations locally first
- Production database is managed by DigitalOcean
- Local database can be reset anytime with Docker