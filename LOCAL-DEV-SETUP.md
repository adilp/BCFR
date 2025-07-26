# Local Development Setup with Docker

This guide helps you set up a local PostgreSQL database for development without affecting your production deployment on DigitalOcean.

## Prerequisites
- Docker Desktop installed
- .NET 8 SDK installed locally

## Setup Steps

### 1. Start Local PostgreSQL
```bash
# From the memberorg-app directory
docker-compose -f docker-compose.local.yml up -d
```

This starts PostgreSQL on port 5433 (to avoid conflicts) with:
- Database: `memberorg_local`
- Username: `local_user`
- Password: `local_password`

### 2. Install Entity Framework Core Tools
```bash
# Install globally (one time only)
dotnet tool install --global dotnet-ef
```

### 3. Update API to Support Both Local and Production

The API needs to detect which environment it's in and use the appropriate database connection.

### 4. Create Your First Migration
```bash
cd apps/api

# Create migration
dotnet ef migrations add InitialCreate

# Apply to local database
dotnet ef database update
```

### 5. Run API Locally
```bash
# Option 1: Use environment file
dotnet run --environment Development

# Option 2: With inline connection string
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=memberorg_local;Username=local_user;Password=local_password" dotnet run
```

## How This Works

### Local Development
- Uses Docker PostgreSQL on port 5433
- Connection string from .env.local or appsettings.Development.json
- Migrations created and tested locally

### Production (DigitalOcean)
- Uses DigitalOcean's managed PostgreSQL
- Connection string from DATABASE_URL environment variable
- Same migrations deployed via git push

## Important Notes

1. **No Production Impact**: This local setup is completely isolated
2. **Git Ignored**: .env.local is git-ignored for security
3. **Port 5433**: Using non-standard port to avoid conflicts
4. **Migration Files**: These ARE committed to git and deployed

## Database Management Commands

```bash
# Start database
docker-compose -f docker-compose.local.yml up -d

# Stop database
docker-compose -f docker-compose.local.yml down

# Stop and remove all data
docker-compose -f docker-compose.local.yml down -v

# View logs
docker-compose -f docker-compose.local.yml logs postgres-local

# Connect with psql
docker exec -it memberorg-postgres-local psql -U local_user -d memberorg_local
```

## Next Steps

1. Create your DbContext and models
2. Configure connection string handling in Program.cs
3. Create initial migration
4. Start building your data models!