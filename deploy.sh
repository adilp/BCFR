#!/bin/bash

# DigitalOcean App Platform Deployment Script

echo "🚀 Deploying to DigitalOcean App Platform..."

# Check if doctl is installed
if ! command -v doctl &> /dev/null; then
    echo "❌ doctl CLI not found. Please install it first:"
    echo "brew install doctl"
    echo "Then authenticate: doctl auth init"
    exit 1
fi

# Check if authenticated
if ! doctl account get &> /dev/null; then
    echo "❌ Not authenticated. Please run: doctl auth init"
    exit 1
fi

# Deploy the app
echo "📦 Creating app from spec..."
doctl apps create --spec .do/app.yaml

# Get the app ID
APP_ID=$(doctl apps list --format ID --no-header | head -1)

if [ -z "$APP_ID" ]; then
    echo "❌ Failed to create app"
    exit 1
fi

echo "✅ App created with ID: $APP_ID"
echo ""
echo "📱 Your app will be available at:"
doctl apps get $APP_ID --format LiveURL --no-header
echo ""
echo "To view logs: doctl apps logs $APP_ID"
echo "To redeploy: doctl apps create-deployment $APP_ID"