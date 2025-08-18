import type { 
  User, 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse 
} from '../types';
import type { AuthStorage } from './storage';

// Storage keys
const STORAGE_KEYS = {
  TOKEN: 'authToken',
  EXPIRES_AT: 'authExpiresAt',
  USER: 'authUser'
} as const;

/**
 * Platform-agnostic authentication manager
 * This can be used by both web and mobile apps
 */
export class AuthManager {
  private apiClient: any; // Will be injected to avoid circular dependency
  private storage: AuthStorage;
  
  constructor(apiClient: any, storage: AuthStorage) {
    this.apiClient = apiClient;
    this.storage = storage;
  }

  // Token management functions
  async getToken(): Promise<string | null> {
    const token = await this.storage.getItem(STORAGE_KEYS.TOKEN);
    const expiresAt = await this.storage.getItem(STORAGE_KEYS.EXPIRES_AT);
    
    if (!token || !expiresAt) {
      return null;
    }

    // Check if token is expired
    if (new Date(expiresAt) <= new Date()) {
      await this.clearAuthData();
      return null;
    }

    return token;
  }

  async clearAuthData(): Promise<void> {
    await this.storage.multiRemove([
      STORAGE_KEYS.TOKEN,
      STORAGE_KEYS.EXPIRES_AT,
      STORAGE_KEYS.USER
    ]);
  }

  async setAuthData(data: AuthResponse): Promise<void> {
    await this.storage.setItem(STORAGE_KEYS.TOKEN, data.token);
    await this.storage.setItem(STORAGE_KEYS.EXPIRES_AT, data.expiresAt);
    
    // Store basic user info from auth response
    const basicUser = {
      username: data.username,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: data.role,
    };
    await this.storage.setItem(STORAGE_KEYS.USER, JSON.stringify(basicUser));
  }

  async updateStoredUser(user: User): Promise<void> {
    await this.storage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
  }

  async getStoredUser(): Promise<Partial<User> | null> {
    const userStr = await this.storage.getItem(STORAGE_KEYS.USER);
    if (!userStr) return null;
    
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }

  // Fetch current user profile with full data
  async fetchUserProfile(): Promise<User | null> {
    try {
      const profileData = await this.apiClient.getProfile();
      
      const user: User = {
        id: profileData.id,
        username: profileData.username,
        email: profileData.email,
        firstName: profileData.firstName,
        lastName: profileData.lastName,
        role: profileData.role as 'Admin' | 'Member',
        dateOfBirth: profileData.dateOfBirth,
        phone: profileData.phone,
        address: profileData.address,
        city: profileData.city,
        state: profileData.state,
        zipCode: profileData.zipCode,
        country: profileData.country,
        isActive: profileData.isActive,
        emailVerified: profileData.emailVerified,
        memberSince: profileData.memberSince,
        lastLoginAt: profileData.lastLoginAt,
        createdAt: profileData.createdAt,
        updatedAt: profileData.updatedAt,
        dietaryRestrictions: profileData.dietaryRestrictions
      };
      
      this.updateStoredUser(user);
      return user;
    } catch (error) {
      console.error('Failed to fetch user profile:', error);
      return null;
    }
  }

  // Authentication methods
  async login(credentials: LoginRequest): Promise<{ user: User | null; response: AuthResponse }> {
    const response = await this.apiClient.login(credentials);
    this.setAuthData(response);
    
    // Fetch full user profile after login
    const user = await this.fetchUserProfile();
    
    return { user, response };
  }

  async register(data: RegisterRequest): Promise<{ user: User | null; response: AuthResponse }> {
    const response = await this.apiClient.register(data);
    this.setAuthData(response);
    
    // Fetch full user profile after registration
    const user = await this.fetchUserProfile();
    
    return { user, response };
  }

  async logout(): Promise<void> {
    try {
      await this.apiClient.logout();
    } catch (error) {
      console.error('Logout API call failed:', error);
    } finally {
      this.clearAuthData();
    }
  }

  async refreshUser(): Promise<User | null> {
    if (await this.getToken()) {
      return await this.fetchUserProfile();
    }
    return null;
  }

  // Role checking utilities
  async isAuthenticated(): Promise<boolean> {
    const token = await this.getToken();
    return !!token;
  }

  checkRole(user: User | null, role: string): boolean {
    return user?.role === role;
  }

  getUserRole(user: User | null): string | null {
    return user?.role || null;
  }
}

// Factory function to create auth manager with proper API client
export function createAuthManager(apiClient: any, storage: AuthStorage): AuthManager {
  return new AuthManager(apiClient, storage);
}