// Re-export all types
export * from './types';

export const APP_NAME = "Member Organization";

// Re-export all date utilities
export * from './utils/dateUtils';

// Re-export validation utilities
export * from './utils/validation';

// Re-export formatting utilities
export * from './utils/formatting';

// Re-export authentication utilities
export { AuthManager, createAuthManager } from './auth/authManager';
export type { AuthStorage } from './auth/storage';
export { 
  WebAuthStorage, 
  MobileAuthStorage, 
  createAuthStorage 
} from './auth/storage';

// Deprecated - use date utilities instead
export const formatDate = (date: Date): string => {
  return date.toLocaleDateString();
};