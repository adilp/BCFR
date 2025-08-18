import { createContext, useContext, type ReactNode } from 'react';
import { useAuthManager } from '../hooks/useAuthManager';
import type { User, LoginRequest, RegisterRequest } from '@memberorg/shared';

// Keep the same interface for backward compatibility
interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  // Additional utilities from the consolidated hook
  isAdmin: boolean;
  isMember: boolean;
  getUserRole: string | null;
  error: string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Use the consolidated authentication hook
  const auth = useAuthManager();

  // Map the auth manager to the context interface
  const contextValue: AuthContextType = {
    user: auth.user,
    isAuthenticated: auth.isAuthenticated,
    isLoading: auth.isLoading,
    login: auth.login,
    register: auth.register,
    logout: auth.logout,
    refreshUser: auth.refreshUser,
    isAdmin: auth.isAdmin,
    isMember: auth.isMember,
    getUserRole: auth.getUserRole,
    error: auth.error,
  };

  return (
    <AuthContext.Provider value={contextValue}>
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