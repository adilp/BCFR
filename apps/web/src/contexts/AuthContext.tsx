import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import authService, { type User, type LoginRequest, type RegisterRequest } from '../services/auth';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if user is authenticated on mount
    const checkAuth = async () => {
      try {
        if (authService.isAuthenticated()) {
          const storedUser = authService.getStoredUser();
          if (storedUser) {
            setUser(storedUser as User);
            // Optionally refresh user data from server
            try {
              const currentUser = await authService.getCurrentUser();
              setUser(currentUser);
            } catch {
              // If fetching current user fails, use stored data
            }
          }
        }
      } finally {
        setIsLoading(false);
      }
    };

    checkAuth();
  }, []);

  const login = async (credentials: LoginRequest) => {
    const response = await authService.login(credentials);
    // After successful login, fetch the full user profile to get the ID
    try {
      const currentUser = await authService.getCurrentUser();
      setUser(currentUser);
    } catch {
      // If fetching profile fails, use data from login response (without ID)
      setUser({
        id: '', // Empty string as fallback
        username: response.username,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        role: response.role || 'Member',
      });
    }
  };

  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    // After successful registration, fetch the full user profile to get the ID
    try {
      const currentUser = await authService.getCurrentUser();
      setUser(currentUser);
    } catch {
      // If fetching profile fails, use data from register response (without ID)
      setUser({
        id: '', // Empty string as fallback
        username: response.username,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        role: response.role || 'Member',
      });
    }
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
  };

  const refreshUser = async () => {
    if (authService.isAuthenticated()) {
      const currentUser = await authService.getCurrentUser();
      setUser(currentUser);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user && authService.isAuthenticated(),
        isLoading,
        login,
        register,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}