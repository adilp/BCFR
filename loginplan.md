# Authentication Implementation Plan

## Overview
Simple username/password authentication using JWT tokens with database session storage.

## Architecture

### Approach: JWT-based Authentication with Database Session Storage

1. **Backend (API) Changes:**
   - Update User model to include username and password hash
   - Create authentication DTOs (LoginRequest, LoginResponse, RegisterRequest)
   - Add BCrypt.Net for password hashing
   - Add JWT bearer authentication package
   - Create AuthController with Login/Register/Logout endpoints
   - Create Session table to store active sessions (userId, token, expiry)
   - Add authentication middleware to protect endpoints

2. **Frontend (Web) Changes:**
   - Create an auth context/store for managing authentication state
   - Store JWT token in localStorage/sessionStorage
   - Add axios interceptor to include auth token in requests
   - Update LoginPage to call authentication API
   - Add protected route wrapper for authenticated pages
   - Create logout functionality
   - Add user info display in navigation

3. **Database Schema:**
   - Update User table: add Username and PasswordHash columns
   - Create Sessions table: Id, UserId, Token, CreatedAt, ExpiresAt
   - Add unique constraint on Username

## Why This Approach?
- **JWT tokens**: Stateless, scalable, and industry standard
- **Database sessions**: Allows server-side session invalidation and tracking
- **BCrypt**: Secure password hashing
- **Simple but extensible**: Easy to add features like refresh tokens later

## Mobile Compatibility
This approach works seamlessly with mobile apps:
- Same API endpoints for all platforms
- Mobile stores JWT in secure storage (Expo SecureStore)
- Sessions table enables cross-device logout and session management
- Can track device-specific information in sessions