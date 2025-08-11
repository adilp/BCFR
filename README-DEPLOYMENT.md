# Important: GitHub Repository Required

App Platform requires a GitHub repository to deploy from. You have two options:

## Option 1: Update app.yaml with your GitHub repo

1. Edit `.do/app.yaml`
2. Replace `YOUR_GITHUB_USERNAME/YOUR_REPO_NAME` with your actual GitHub repository
3. Run: `doctl apps create --spec .do/app.yaml`

## Option 2: Use the DigitalOcean UI

1. Push your code to GitHub first
2. Go to https://cloud.digitalocean.com/apps
3. Click "Create App"
4. Connect your GitHub account
5. Select your repository
6. App Platform will auto-detect the monorepo structure

## Option 3: Create without spec file (simpler)

```bash
# This will walk you through setup interactively
doctl apps create
```

Then manually configure:
- Source: Your GitHub repo
- API component: Set Dockerfile path to `apps/api/Dockerfile`
- Web component: Set as static site with build command `npm install && npm run build`

## Next Steps

1. Create a GitHub repository if you haven't already
2. Push your code:
   ```bash
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
   git push -u origin main
   ```
3. Then deploy using one of the options above