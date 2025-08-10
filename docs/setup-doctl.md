# Quick Setup Guide

## 1. Install doctl CLI
```bash
# macOS
brew install doctl

# Or download from: https://docs.digitalocean.com/reference/doctl/how-to/install/
```

## 2. Authenticate
```bash
doctl auth init
```
You'll need a DigitalOcean API token from: https://cloud.digitalocean.com/account/api/tokens

## 3. Deploy
```bash
cd memberorg-app
./deploy.sh
```

## Alternative: Direct Command
```bash
# Deploy directly without script
doctl apps create --spec .do/app.yaml
```

## Delete Existing App (if needed)
```bash
# List apps
doctl apps list

# Delete app
doctl apps delete <app-id>
```