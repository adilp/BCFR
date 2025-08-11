# DigitalOcean App Platform Deployment Guide

## Overview
This guide covers deploying the memberorg monorepo to DigitalOcean App Platform with:
- C# ASP.NET Core API (Docker container)
- React Web App (Static site)
- PostgreSQL Database (Managed)

## Prerequisites
1. DigitalOcean account
2. GitHub repository with your code
3. Doctl CLI (optional but recommended)

## Database Options on App Platform

### Option 1: Dev Database (Free - Recommended for Development)
- **PostgreSQL 15**: FREE (db-s-dev-database)
- Limited to 100MB storage
- No backups
- Perfect for development/testing
- Automatically expires after 90 days of inactivity
- Can be upgraded to production database later

### Option 2: Managed Database (Production)
- **PostgreSQL 15**: $15/month (basic-xs)
- Automatic backups
- SSL connections
- Connection pooling
- Scales independently

### Option 3: External Database
- Use existing DigitalOcean Managed Database
- Use external provider (Supabase, Neon, etc.)
- Update DATABASE_URL in app settings

## Deployment Steps

### 1. Prepare Your Code

#### Update API for Production
```csharp
// Program.cs - Add database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=memberorg;Username=postgres;Password=postgres"
  }
}
```

#### Update Web App API URL
```typescript
// apps/web/src/App.tsx
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001';
const apiClient = new ApiClient(apiUrl);
```

### 2. Create App via CLI or UI

#### Using CLI:
```bash
doctl apps create --spec .do/app.yaml
```

#### Using UI:
1. Go to https://cloud.digitalocean.com/apps
2. Click "Create App"
3. Connect GitHub repository
4. Use app.yaml spec

### 3. Configure Environment Variables

Required environment variables:
- `DATABASE_URL`: Automatically set by App Platform
- `ASPNETCORE_ENVIRONMENT`: Production
- `CORS_ORIGINS`: Your app URL
- `VITE_API_URL`: ${APP_URL}/api (build-time)

### 4. Deploy

```bash
# Deploy using CLI
doctl apps create-deployment <app-id>

# Or push to main branch (auto-deploy)
git push origin main
```

## Post-Deployment

### 1. Run Database Migrations
```bash
# Connect to app console
doctl apps console <app-id> api

# Run migrations
dotnet ef database update
```

### 2. Configure Custom Domain
1. Go to Settings → Domains
2. Add your domain
3. Update DNS records

### 3. Enable HTTPS
- Automatic with custom domains
- Force HTTPS in settings

## Cost Breakdown

### Development Setup (~$5/month):
- API Service: $5/month (basic-xxs)
- Database: FREE (dev database)
- Static Site: Free

### Basic Production Setup (~$20/month):
- API Service: $5/month (basic-xxs)
- Database: $15/month (basic-xs PostgreSQL)
- Static Site: Free

### Scaled Production Setup (~$72/month):
- API Service: $12/month (basic-s)
- Database: $60/month (professional-xs)
- Static Site: Free

## Monitoring

1. **Logs**: Apps → Your App → Runtime Logs
2. **Metrics**: Apps → Your App → Insights
3. **Alerts**: Settings → Alerts

## Troubleshooting

### API Can't Connect to Database
- Check DATABASE_URL is set
- Verify database is in same region
- Check connection string format

### CORS Issues
- Verify CORS_ORIGINS includes your domain
- Check API CORS configuration

### Build Failures
- Check build logs
- Verify Node version (specify in package.json)
- Ensure all dependencies are listed

## Next Steps

1. Set up CI/CD with GitHub Actions
2. Configure health checks
3. Add monitoring (Sentry, etc.)
4. Set up staging environment
5. Configure auto-scaling rules
