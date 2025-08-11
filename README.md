# Member Organization Monorepo

This monorepo contains:
- React Native app (Expo)
- React web app (Vite)
- C# ASP.NET Core API
- Shared packages for code reuse

## How to Run

### Start everything at once:
```bash
yarn dev
```

### Or start individually:

#### Terminal 1 - API:
```bash
yarn api
# API runs at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

#### Terminal 2 - Web:
```bash
yarn web
# Web app runs at http://localhost:5173
```

#### Terminal 3 - Mobile:
```bash
yarn mobile
# Expo starts and shows QR code
# Press 'w' for web, 'i' for iOS simulator, 'a' for Android
```

## Troubleshooting

1. **Mobile can't connect to API**: Use your computer's IP address instead of localhost in the mobile app
2. **Port conflicts**: Make sure ports 5000 (API) and 5173 (web) are free
3. **CORS errors**: The API is configured to allow all origins in development