import { useState, useEffect, useCallback, useMemo } from 'react';
import { getApiClient } from '@memberorg/api-client';
import { 
  createAuthManager, 
  createAuthStorage
} from '@memberorg/shared';
import type { User, LoginRequest, RegisterRequest } from '@memberorg/shared';

/**
 * React hook that wraps the shared AuthManager for use in React components
 * This provides a stateful interface with React-specific features
 */
export function useAuthManager() {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  // Create auth manager instance with API client and storage
  const authManager = useMemo(() => {
    const apiClient = getApiClient();
    const storage = createAuthStorage();
    return createAuthManager(apiClient, storage);
  }, []);

  // Check authentication status
  const checkAuth = useCallback(async () => {
    const authenticated = await authManager.isAuthenticated();
    setIsAuthenticated(authenticated);
    return authenticated;
  }, [authManager]);

  // Authentication methods wrapped with React state updates
  const login = useCallback(async (credentials: LoginRequest): Promise<void> => {
    setError(null);
    try {
      const { user: loggedInUser, response } = await authManager.login(credentials);
      
      if (loggedInUser) {
        setUser(loggedInUser);
      } else {
        // Fallback to basic user data from auth response
        setUser({
          id: '', // Will be populated when profile is fetched
          username: response.username,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: (response.role || 'Member') as 'Admin' | 'Member',
          isActive: true,
          emailVerified: false,
          createdAt: new Date().toISOString()
        });
      }
      
      await checkAuth();
    } catch (error: any) {
      setError(error.message || 'Login failed');
      throw error;
    }
  }, [authManager, checkAuth]);

  const register = useCallback(async (data: RegisterRequest): Promise<void> => {
    setError(null);
    try {
      const { user: registeredUser, response } = await authManager.register(data);
      
      if (registeredUser) {
        setUser(registeredUser);
      } else {
        // Fallback to basic user data from auth response
        setUser({
          id: '', // Will be populated when profile is fetched
          username: response.username,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: (response.role || 'Member') as 'Admin' | 'Member',
          isActive: true,
          emailVerified: false,
          createdAt: new Date().toISOString()
        });
      }
      
      await checkAuth();
    } catch (error: any) {
      setError(error.message || 'Registration failed');
      throw error;
    }
  }, [authManager, checkAuth]);

  const logout = useCallback(async (): Promise<void> => {
    await authManager.logout();
    setUser(null);
    setIsAuthenticated(false);
  }, [authManager]);

  const refreshUser = useCallback(async (): Promise<void> => {
    const refreshedUser = await authManager.refreshUser();
    if (refreshedUser) {
      setUser(refreshedUser);
    }
    await checkAuth();
  }, [authManager, checkAuth]);

  const getToken = useCallback(async (): Promise<string | null> => {
    return await authManager.getToken();
  }, [authManager]);

  // Role checking utilities
  const isAdmin = useMemo(() => authManager.checkRole(user, 'Admin'), [authManager, user]);
  const isMember = useMemo(() => authManager.checkRole(user, 'Member'), [authManager, user]);
  const getUserRole = useMemo(() => authManager.getUserRole(user), [authManager, user]);

  // Initialize auth state on mount
  useEffect(() => {
    const initAuth = async () => {
      setIsLoading(true);
      try {
        const authenticated = await authManager.isAuthenticated();
        
        if (authenticated) {
          // Try to get stored user first for immediate UI update
          const storedUser = await authManager.getStoredUser();
          if (storedUser && storedUser.id) {
            setUser(storedUser as User);
          }
          
          // Then fetch fresh profile data from server
          const freshUser = await authManager.fetchUserProfile();
          if (freshUser) {
            setUser(freshUser);
          }
        }
        
        setIsAuthenticated(authenticated);
      } catch (error) {
        console.error('Failed to initialize auth:', error);
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();
  }, [authManager]);

  return {
    // State
    user,
    isAuthenticated,
    isLoading,
    error,
    
    // Auth methods
    login,
    register,
    logout,
    refreshUser,
    
    // Token management
    getToken,
    
    // Role checks
    isAdmin,
    isMember,
    getUserRole,
  };
}

// Export type for the hook's return value
export type AuthManagerHook = ReturnType<typeof useAuthManager>;