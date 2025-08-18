/**
 * Storage abstraction interface for authentication data
 * This allows different implementations for web (localStorage) and mobile (AsyncStorage)
 */
export interface AuthStorage {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
  multiRemove(keys: string[]): Promise<void>;
}

/**
 * Web implementation using localStorage
 */
export class WebAuthStorage implements AuthStorage {
  async getItem(key: string): Promise<string | null> {
    return localStorage.getItem(key);
  }

  async setItem(key: string, value: string): Promise<void> {
    localStorage.setItem(key, value);
  }

  async removeItem(key: string): Promise<void> {
    localStorage.removeItem(key);
  }

  async multiRemove(keys: string[]): Promise<void> {
    keys.forEach(key => localStorage.removeItem(key));
  }
}

/**
 * Mobile implementation stub (will use AsyncStorage when implemented)
 */
export class MobileAuthStorage implements AuthStorage {
  private storage: Map<string, string> = new Map();

  async getItem(key: string): Promise<string | null> {
    // In real implementation, this would use React Native's AsyncStorage
    return this.storage.get(key) || null;
  }

  async setItem(key: string, value: string): Promise<void> {
    // In real implementation, this would use React Native's AsyncStorage
    this.storage.set(key, value);
  }

  async removeItem(key: string): Promise<void> {
    // In real implementation, this would use React Native's AsyncStorage
    this.storage.delete(key);
  }

  async multiRemove(keys: string[]): Promise<void> {
    // In real implementation, this would use React Native's AsyncStorage
    keys.forEach(key => this.storage.delete(key));
  }
}

/**
 * Factory function to get appropriate storage based on platform
 */
export function createAuthStorage(): AuthStorage {
  // Check if we're in a web environment
  if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
    return new WebAuthStorage();
  }
  
  // Default to mobile storage for React Native
  return new MobileAuthStorage();
}