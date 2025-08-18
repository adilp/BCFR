import api from './api';
import { getApiClient } from '@memberorg/api-client';
import type { User as SharedUser, LoginRequest as SharedLoginRequest, RegisterRequest as SharedRegisterRequest, AuthResponse as SharedAuthResponse } from '@memberorg/shared';

// Re-export types from shared package for backward compatibility
export type LoginRequest = SharedLoginRequest;
export type RegisterRequest = SharedRegisterRequest;
export type AuthResponse = SharedAuthResponse;
export type User = SharedUser;

class AuthService {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    console.log('AuthService.login called with:', credentials);
    try {
      // Try using the new API client first
      const apiClient = getApiClient();
      const response = await apiClient.login(credentials);
      this.setAuthData(response);
      return response;
    } catch (error) {
      // Fallback to old method if API client not initialized
      const response = await api.post<AuthResponse>('/auth/login', credentials);
      console.log('Login response:', response.data);
      this.setAuthData(response.data);
      return response.data;
    }
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    try {
      const apiClient = getApiClient();
      const response = await apiClient.register(data);
      this.setAuthData(response);
      return response;
    } catch (error) {
      // Fallback to old method
      const response = await api.post<AuthResponse>('/auth/register', data);
      this.setAuthData(response.data);
      return response.data;
    }
  }

  async logout(): Promise<void> {
    try {
      await api.post('/auth/logout');
    } finally {
      this.clearAuthData();
    }
  }

  async getCurrentUser(): Promise<User> {
    try {
      const apiClient = getApiClient();
      const profileData = await apiClient.getProfile();
      console.log('Profile data from API:', profileData);
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
      console.log('User object to be stored:', user);
      // Update stored user with full data including ID
      this.updateStoredUser(user);
      return user;
    } catch (error) {
      // Fallback to old method
      const response = await api.get('/profile');
      const profileData = response.data;
      const user: User = {
        id: profileData.id,
        username: profileData.username,
        email: profileData.email,
        firstName: profileData.firstName,
        lastName: profileData.lastName,
        role: this.getUserRole() as 'Admin' | 'Member' || 'Member',
        dateOfBirth: profileData.dateOfBirth,
        isActive: true,
        createdAt: profileData.createdAt
      };
      this.updateStoredUser(user);
      return user;
    }
  }

  getToken(): string | null {
    const token = localStorage.getItem('authToken');
    const expiresAt = localStorage.getItem('authExpiresAt');
    
    if (!token || !expiresAt) {
      return null;
    }

    // Check if token is expired
    if (new Date(expiresAt) <= new Date()) {
      this.clearAuthData();
      return null;
    }

    return token;
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  getStoredUser(): User | null {
    const userStr = localStorage.getItem('authUser');
    if (!userStr) return null;
    
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }

  getUserRole(): string | null {
    const user = this.getStoredUser();
    return user?.role || null;
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'Admin';
  }

  isMember(): boolean {
    return this.getUserRole() === 'Member';
  }

  private setAuthData(data: AuthResponse): void {
    localStorage.setItem('authToken', data.token);
    localStorage.setItem('authExpiresAt', data.expiresAt);
    localStorage.setItem('authUser', JSON.stringify({
      username: data.username,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: data.role,
    }));
  }

  private updateStoredUser(user: User): void {
    localStorage.setItem('authUser', JSON.stringify(user));
  }

  private clearAuthData(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('authExpiresAt');
    localStorage.removeItem('authUser');
  }
}

export default new AuthService();