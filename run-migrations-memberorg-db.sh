#!/bin/bash

# Migration script for memberorg-db database
# This uses the memberorg-db database instead of defaultdb

echo "Running migrations on memberorg-db database..."

# Export the connection string for memberorg-db database
# Replace the password with the actual password from DigitalOcean
export DATABASE_URL="postgresql://memberorg-db:YOUR_PASSWORD_HERE@app-52d511e1-3c1a-4fdf-af95-f3a85bdc7b06-do-user-1995674-0.i.db.ondigitalocean.com:25060/memberorg-db?sslmode=require"

# Navigate to the API directory
cd apps/api

# Run the migrations
dotnet ef database update -- --environment Production

echo "Migrations complete!"