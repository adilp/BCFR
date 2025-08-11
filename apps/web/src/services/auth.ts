import api from './api';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  expiresAt: string;
}

export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  dateOfBirth?: string;
}

class AuthService {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    console.log('AuthService.login called with:', credentials);
    const response = await api.post<AuthResponse>('/auth/login', credentials);
    console.log('Login response:', response.data);
    this.setAuthData(response.data);
    return response.data;
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', data);
    this.setAuthData(response.data);
    return response.data;
  }

  async logout(): Promise<void> {
    try {
      await api.post('/auth/logout');
    } finally {
      this.clearAuthData();
    }
  }

  async getCurrentUser(): Promise<User> {
    const response = await api.get<User>('/auth/me');
    return response.data;
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem('authToken');
    const expiresAt = localStorage.getItem('authExpiresAt');
    
    if (!token || !expiresAt) {
      return false;
    }

    return new Date(expiresAt) > new Date();
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

  private clearAuthData(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('authExpiresAt');
    localStorage.removeItem('authUser');
  }
}

export default new AuthService();